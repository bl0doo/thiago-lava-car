import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Users } from 'lucide-react';
import { useState } from 'react';
import {
  alterarAtivo,
  criarLavador,
  editarDiaria,
  editarMembro,
  excluirDiaria,
  lancarDiariasDoDia,
  listarDiarias,
  listarEquipe,
  redefinirPin,
  type Diaria,
  type Membro,
} from '@/api/equipe';
import { interpretarErro } from '@/api/erros';
import { BarraTitulo } from '@/components/layout/BarraTitulo';
import { Alerta } from '@/components/ui/Alerta';
import { Button } from '@/components/ui/Button';
import { Caixinha } from '@/components/ui/Caixinha';
import { Campo } from '@/components/ui/Campo';
import { Carregando } from '@/components/ui/Carregando';
import { ErroConsulta } from '@/components/ui/ErroConsulta';
import { Dialogo } from '@/components/ui/Dialogo';
import { EstadoVazio } from '@/components/ui/EstadoVazio';
import { formatarDataIso, formatarMoeda, hojeIso, lerValor } from '@/lib/format';
import { editarMembroSchema, novoLavadorSchema } from '@/schemas/equipe';
import { pinSchema } from '@/schemas/comuns';

type Acao =
  | { tipo: 'pin' | 'desativar' | 'editar'; lavador: Membro }
  | { tipo: 'novo' }
  | { tipo: 'excluirDiaria'; diaria: Diaria }
  | null;

const rotulo = (m: Membro) => m.apelido ?? m.nome;
const FORM_VAZIO = { nome: '', apelido: '', pin: '' };

/** Equipe e diárias (`/dono/equipe`): PIN e acesso dos lavadores e lançamento das diárias do dia. */
export function Equipe() {
  const qc = useQueryClient();
  const hoje = hojeIso();
  const [erro, setErro] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [acao, setAcao] = useState<Acao>(null);
  const [pin, setPin] = useState('');
  const [erroPin, setErroPin] = useState<string | null>(null);
  const [form, setForm] = useState(FORM_VAZIO);
  const [errosForm, setErrosForm] = useState<Record<string, string>>({});
  const [dia, setDia] = useState(hoje);
  const [valorTexto, setValorTexto] = useState('');
  const [erroValor, setErroValor] = useState<string | null>(null);
  const [marcados, setMarcados] = useState<number[]>([]);
  const mes = dia.slice(0, 7);

  const equipe = useQuery({ queryKey: ['equipe'], queryFn: listarEquipe });
  const ativos = equipe.data?.filter((m) => m.ativo);
  const diarias = useQuery({ queryKey: ['diarias', mes], queryFn: () => listarDiarias(mes) });
  const nomeDe = (id: number) => (() => {
      const m = equipe.data?.find((x) => x.id === id);
      return m ? rotulo(m) : `Lavador ${id}`;
    })();

  const falhou = (e: unknown) => setErro(interpretarErro(e).mensagem);
  const fechar = () => {
    setAcao(null);
    setPin('');
    setErroPin(null);
    setForm(FORM_VAZIO);
    setErrosForm({});
  };

  const trocarPin = useMutation({
    mutationFn: ({ id }: { id: number }) => redefinirPin(id, pin),
    onSuccess: () => {
      setAviso('PIN redefinido. O lavador precisa entrar de novo.');
      fechar();
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      setErroPin(campos.pin ?? mensagem);
    },
  });

  const mudarAcesso = useMutation({
    mutationFn: ({ id, ativo }: { id: number; ativo: boolean }) => alterarAtivo(id, ativo),
    onMutate: () => setErro(null),
    onSuccess: (_r, { ativo }) => {
      setAviso(ativo ? 'Acesso reativado.' : 'Acesso desativado.');
      void qc.invalidateQueries({ queryKey: ['equipe'] });
    },
    onError: falhou,
    onSettled: fechar,
  });

  const salvarMembro = useMutation({
    mutationFn: () => (acao?.tipo === 'editar' ? editarMembro(acao.lavador.id, form.nome.trim(), form.apelido.trim()) : criarLavador({ nome: form.nome.trim(), apelido: form.apelido.trim(), pin: form.pin })),
    onSuccess: () => {
      setAviso(acao?.tipo === 'editar' ? 'Dados salvos.' : 'Lavador cadastrado.');
      void qc.invalidateQueries({ queryKey: ['equipe'] });
      fechar();
    },
    onError: (e) => {
      const { campos, mensagem } = interpretarErro(e);
      const conhecidos: Record<string, string> = {};
      for (const c of ['nome', 'apelido', 'pin']) if (campos[c]) conhecidos[c] = campos[c];
      if (Object.keys(conhecidos).length) setErrosForm(conhecidos);
      else setErrosForm({ apelido: mensagem });
    },
  });

  const confirmarMembro = () => {
    const r = (acao?.tipo === 'editar' ? editarMembroSchema : novoLavadorSchema).safeParse(form);
    if (!r.success) {
      setErrosForm(Object.fromEntries(r.error.issues.map((i) => [String(i.path[0]), i.message])));
      return;
    }
    setErrosForm({});
    salvarMembro.mutate();
  };

  const lancar = useMutation({
    mutationFn: () => lancarDiariasDoDia(dia, marcados, valorTexto.trim() ? lerValor(valorTexto) : null),
    onMutate: () => {
      setErro(null);
      setAviso(null);
    },
    onSuccess: (r) => {
      const ja = r.jaLancadas.length ? ` Já tinham diária: ${r.jaLancadas.map(nomeDe).join(', ')}.` : '';
      setAviso(`${r.criadas.length} ${r.criadas.length === 1 ? 'diária lançada' : 'diárias lançadas'}.${ja}`);
      setMarcados([]);
      void qc.invalidateQueries({ queryKey: ['diarias'] });
      void qc.invalidateQueries({ queryKey: ['caixa'] });
    },
    onError: falhou,
  });

  const pagar = useMutation({
    mutationFn: (d: Diaria) => editarDiaria(d.id, d.valor, true),
    onMutate: () => setErro(null),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['diarias'] }),
    onError: falhou,
  });

  const excluir = useMutation({
    mutationFn: (id: number) => excluirDiaria(id),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['diarias'] });
      void qc.invalidateQueries({ queryKey: ['caixa'] });
    },
    onError: falhou,
    onSettled: fechar,
  });

  const enviarLancamento = () => {
    if (marcados.length === 0) {
      setErro('Escolha ao menos um lavador.');
      return;
    }
    if (valorTexto.trim() && !(lerValor(valorTexto) > 0)) {
      setErroValor('O valor da diária deve ser maior que zero.');
      return;
    }
    setErroValor(null);
    lancar.mutate();
  };

  const confirmarPin = () => {
    if (acao?.tipo !== 'pin') return;
    const r = pinSchema.safeParse(pin);
    if (!r.success) {
      setErroPin(r.error.issues[0]?.message ?? 'O PIN deve ter 4 dígitos.');
      return;
    }
    setErroPin(null);
    trocarPin.mutate({ id: acao.lavador.id });
  };

  return (
    <div className="flex flex-col">
      <BarraTitulo sobretitulo="Pessoas" titulo="Equipe e diárias" />
      <div className="flex flex-col gap-md px-md pb-lg lg:max-w-[40rem]">
        {erro ? <Alerta>{erro}</Alerta> : null}
        {aviso ? <Alerta tipo="sucesso">{aviso}</Alerta> : null}

        <section aria-label="Lavadores" className="flex flex-col gap-xs rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
          <div className="flex items-center justify-between gap-sm">
            <h2 className="texto-h2">Lavadores</h2>
            <Button
              onClick={() => {
                setForm(FORM_VAZIO);
                setAcao({ tipo: 'novo' });
              }}
            >
              Novo lavador
            </Button>
          </div>
          {equipe.isPending ? <Carregando cartoes={1} /> : null}
          {equipe.isError ? (
            <ErroConsulta erro={equipe.error} aoTentarNovamente={equipe.refetch} />
          ) : null}
          {equipe.data?.length === 0 ? (
            <EstadoVazio Icone={Users} titulo="Nenhum lavador cadastrado">
              Use "Novo lavador" para dar acesso a quem trabalha com você.
            </EstadoVazio>
          ) : null}
          <ul className="flex flex-col gap-xs">
            {equipe.data?.map((m) => (
              <li key={m.id} className="flex flex-wrap items-center justify-between gap-xs rounded-md border-2 border-padrao p-sm">
                <div className="flex flex-col">
                  <span className="texto-rotulo-m">{rotulo(m)}</span>
                  <span className="texto-corpo-p text-secundario">
                    {m.nome} · {m.ativo ? 'Ativo' : 'Desativado'}
                  </span>
                </div>
                <div className="flex flex-wrap gap-xs">
                  <Button
                    variante="contorno"
                    aria-label={`Editar ${rotulo(m)}`}
                    onClick={() => {
                      setForm({ nome: m.nome, apelido: m.apelido ?? '', pin: '' });
                      setAcao({ tipo: 'editar', lavador: m });
                    }}
                  >
                    Editar
                  </Button>
                  <Button variante="contorno" aria-label={`Redefinir PIN de ${rotulo(m)}`} onClick={() => setAcao({ tipo: 'pin', lavador: m })}>
                    Redefinir PIN
                  </Button>
                  {m.ativo ? (
                    <Button variante="contorno" aria-label={`Desativar ${rotulo(m)}`} onClick={() => setAcao({ tipo: 'desativar', lavador: m })}>
                      Desativar
                    </Button>
                  ) : (
                    <Button variante="contorno" aria-label={`Reativar ${rotulo(m)}`} disabled={mudarAcesso.isPending} onClick={() => mudarAcesso.mutate({ id: m.id, ativo: true })}>
                      Reativar
                    </Button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </section>

        <section aria-label="Lançar diárias" className="flex flex-col gap-sm rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
          <h2 className="texto-h2">Lançar diárias do dia</h2>
          <Campo rotulo="Dia" type="date" max={hoje} value={dia} onChange={(e) => e.target.value && setDia(e.target.value)} />
          <fieldset className="flex flex-col">
            <legend className="texto-rotulo-m">Quem trabalhou</legend>
            {ativos?.map((l) => (
              <Caixinha
                key={l.id}
                checked={marcados.includes(l.id)}
                onChange={(e) => setMarcados((atual) => (e.target.checked ? [...atual, l.id] : atual.filter((x) => x !== l.id)))}
              >
                {rotulo(l)}
              </Caixinha>
            ))}
          </fieldset>
          <Campo
            rotulo="Valor da diária (opcional)"
            inputMode="decimal"
            value={valorTexto}
            onChange={(e) => setValorTexto(e.target.value)}
            erro={erroValor ?? undefined}
            ajuda="Em branco, vale a diária padrão: R$ 100 na semana e R$ 120 aos sábados."
          />
          <Button carregando={lancar.isPending} onClick={enviarLancamento}>
            Lançar diárias
          </Button>
        </section>

        <section aria-label="Diárias do mês" className="flex flex-col gap-xs rounded-lg border-2 border-forte bg-superficie p-md shadow-seca">
          <h2 className="texto-h2">Diárias de {formatarDataIso(`${mes}-01`).slice(3)}</h2>
          {diarias.isPending ? <Carregando cartoes={1} /> : null}
          {diarias.isError ? (
            <ErroConsulta erro={diarias.error} aoTentarNovamente={diarias.refetch} />
          ) : null}
          {diarias.data ? (
            <>
              <p className="flex justify-between texto-rotulo-m">
                <span>Total do mês</span>
                <span className="texto-mono-dado">{formatarMoeda(diarias.data.total)}</span>
              </p>
              {diarias.data.itens.length === 0 ? <p className="texto-corpo-m text-secundario">Nenhuma diária lançada neste mês.</p> : null}
              <ul className="flex flex-col gap-xs">
                {diarias.data.itens.map((d) => (
                  <li key={d.id} className="flex flex-wrap items-center justify-between gap-xs rounded-md border-2 border-padrao p-sm">
                    <div className="flex flex-col">
                      <span className="texto-rotulo-m">
                        {nomeDe(d.lavadorId)} · {formatarMoeda(d.valor)}
                      </span>
                      <span className="texto-corpo-p text-secundario">
                        {formatarDataIso(d.data)} · {d.paga ? 'Paga' : 'A pagar'}
                      </span>
                    </div>
                    <div className="flex gap-xs">
                      {d.paga ? null : (
                        <Button variante="secundario" aria-label={`Marcar diária de ${nomeDe(d.lavadorId)} de ${formatarDataIso(d.data)} como paga`} disabled={pagar.isPending} onClick={() => pagar.mutate(d)}>
                          Marcar paga
                        </Button>
                      )}
                      <Button variante="contorno" aria-label={`Excluir diária de ${nomeDe(d.lavadorId)} de ${formatarDataIso(d.data)}`} onClick={() => setAcao({ tipo: 'excluirDiaria', diaria: d })}>
                        Excluir
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            </>
          ) : null}
        </section>
      </div>

      <Dialogo
        aberto={acao?.tipo === 'pin'}
        titulo={acao?.tipo === 'pin' ? `Novo PIN de ${rotulo(acao.lavador)}` : ''}
        confirmar="Salvar PIN"
        carregando={trocarPin.isPending}
        onConfirmar={confirmarPin}
        onFechar={fechar}
      >
        <Campo rotulo="PIN de 4 dígitos" inputMode="numeric" maxLength={4} autoComplete="off" value={pin} onChange={(e) => setPin(e.target.value.replace(/\D/g, ''))} erro={erroPin ?? undefined} />
      </Dialogo>

      <Dialogo
        aberto={acao?.tipo === 'desativar'}
        titulo={acao?.tipo === 'desativar' ? `Desativar ${rotulo(acao.lavador)}?` : ''}
        confirmar="Desativar"
        tipo="perigo"
        carregando={mudarAcesso.isPending}
        onConfirmar={() => acao?.tipo === 'desativar' && mudarAcesso.mutate({ id: acao.lavador.id, ativo: false })}
        onFechar={fechar}
      >
        O acesso é encerrado agora e o nome sai da tela de entrada. As diárias e os serviços já feitos continuam no histórico.
      </Dialogo>

      <Dialogo
        aberto={acao?.tipo === 'novo' || acao?.tipo === 'editar'}
        titulo={acao?.tipo === 'editar' ? `Editar ${rotulo(acao.lavador)}` : 'Novo lavador'}
        confirmar={acao?.tipo === 'editar' ? 'Salvar' : 'Cadastrar'}
        carregando={salvarMembro.isPending}
        onConfirmar={confirmarMembro}
        onFechar={fechar}
      >
        <div className="flex flex-col gap-sm">
          <Campo rotulo="Nome completo" value={form.nome} maxLength={100} onChange={(e) => setForm({ ...form, nome: e.target.value })} erro={errosForm.nome} />
          <Campo rotulo="Apelido" value={form.apelido} maxLength={30} onChange={(e) => setForm({ ...form, apelido: e.target.value })} erro={errosForm.apelido} ajuda="É o nome que aparece na tela de entrada." />
          {acao?.tipo === 'novo' ? (
            <Campo
              rotulo="PIN de 4 dígitos"
              inputMode="numeric"
              maxLength={4}
              autoComplete="off"
              value={form.pin}
              onChange={(e) => setForm({ ...form, pin: e.target.value.replace(/\D/g, '') })}
              erro={errosForm.pin}
              ajuda="Evite sequências como 1234 ou 0000."
            />
          ) : null}
        </div>
      </Dialogo>

      <Dialogo
        aberto={acao?.tipo === 'excluirDiaria'}
        titulo="Excluir esta diária?"
        confirmar="Excluir"
        tipo="perigo"
        carregando={excluir.isPending}
        onConfirmar={() => acao?.tipo === 'excluirDiaria' && excluir.mutate(acao.diaria.id)}
        onFechar={fechar}
      >
        A diária sai da lista e do total do mês.
      </Dialogo>
    </div>
  );
}
