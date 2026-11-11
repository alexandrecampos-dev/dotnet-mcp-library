namespace LibraryMcp.Tools.Ferramentas;

// Respostas enxutas: só o que o modelo precisa para a próxima decisão.
// Nada de Id interno, nada de entidade inteira, nada de dado pessoal de outro leitor.

public sealed record LivroResumo(string Isbn, string Titulo, string Autor, int Ano, int Disponiveis, int Exemplares);

public sealed record ResultadoBusca(int Total, IReadOnlyList<LivroResumo> Livros, string? Observacao);

public sealed record EmprestimoResumo(string Isbn, string Titulo, DateOnly DevolverAte, int DiasDeAtraso);

public sealed record MeusEmprestimos(IReadOnlyList<EmprestimoResumo> Ativos, int Limite, int PodePegarMais);

public sealed record ComprovanteDeEmprestimo(string Titulo, string Isbn, DateOnly DevolverAte, int EmprestimosAtivos, int Limite);

public sealed record ComprovanteDeDevolucao(string Titulo, string Isbn, int DiasDeAtraso);

public sealed record ResultadoDoCadastro(string Isbn, string Titulo, int Exemplares, int Disponiveis, bool LivroNovo);
