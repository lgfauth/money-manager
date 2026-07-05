using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Seeds;

// Categoria de referência do Open Banking (Pluggy).
public record OpenBankingCategoryDefinition(
    string Id,                    // categoryId do Pluggy (ex: "07020002")
    string Description,           // descrição em inglês
    string DescriptionTranslated, // descrição em pt-BR — usada como Name da Category
    string? ParentId,
    string? ParentDescription);

// Fonte estática das 130 categorias do Open Banking (Pluggy) usadas na migração
// de categorias dos usuários. Dados do endpoint openfinance_list_categories.
public static class OpenBankingCategorySeed
{
    // Categoria fallback usada quando o categoryId da transação não existe na base local.
    public const string FallbackCategoryId = "99999999";

    // Grupos-raiz de receita: Income ("01") e Investments ("03"). Todos os demais são despesa.
    // Como todo id compartilha os 2 primeiros dígitos do seu grupo-raiz, a derivação por
    // prefixo garante que categorias-pai e filhas tenham sempre o mesmo Type.
    public static CategoryType ResolveType(string openBankingCategoryId)
    {
        var group = GroupPrefix(openBankingCategoryId);
        return group is "01" or "03" ? CategoryType.Income : CategoryType.Expense;
    }

    // Paleta padrão: uma cor por grupo-raiz, para as categorias do grupo manterem identidade visual.
    public static string ResolveColor(string openBankingCategoryId)
    {
        return GroupColors.TryGetValue(GroupPrefix(openBankingCategoryId), out var color)
            ? color
            : DefaultColor;
    }

    private static string GroupPrefix(string openBankingCategoryId) =>
        openBankingCategoryId.Length >= 2 ? openBankingCategoryId[..2] : openBankingCategoryId;

    private const string DefaultColor = "#95A5A6";

    private static readonly IReadOnlyDictionary<string, string> GroupColors = new Dictionary<string, string>
    {
        ["01"] = "#27AE60", // Renda
        ["02"] = "#C0392B", // Empréstimos e financiamento
        ["03"] = "#16A085", // Investimentos
        ["04"] = "#2980B9", // Transferência mesma titularidade
        ["05"] = "#3498DB", // Transferências
        ["06"] = "#7F8C8D", // Obrigações legais
        ["07"] = "#8E44AD", // Serviços
        ["08"] = "#E67E22", // Compras
        ["09"] = "#9B59B6", // Serviços digitais
        ["10"] = "#2ECC71", // Supermercado
        ["11"] = "#E74C3C", // Alimentos e bebidas
        ["12"] = "#1ABC9C", // Viagens
        ["13"] = "#F1C40F", // Doações
        ["14"] = "#D35400", // Apostas
        ["15"] = "#34495E", // Impostos
        ["16"] = "#95A5A6", // Taxas bancárias
        ["17"] = "#E84393", // Moradia
        ["18"] = "#00B894", // Saúde
        ["19"] = "#0984E3", // Transporte
        ["20"] = "#6C5CE7", // Seguros
        ["21"] = "#FDCB6E", // Lazer
        ["99"] = "#B2BEC3"  // Outros
    };

    public static IReadOnlyList<OpenBankingCategoryDefinition> Categories { get; } =
    [
        new("01000000", "Income", "Renda", null, null),
        new("01010000", "Salary", "Salário", "01000000", "Income"),
        new("01020000", "Retirement", "Aposentadoria", "01000000", "Income"),
        new("01030000", "Entrepreneurial activities", "Atividades de empreendedorismo", "01000000", "Income"),
        new("01040000", "Government aid", "Auxílio do governo", "01000000", "Income"),
        new("01050000", "Non-recurring income", "Renda não-recorrente", "01000000", "Income"),
        new("02000000", "Loans and financing", "Empréstimos e financiamento", null, null),
        new("02010000", "Late payment and overdraft costs", "Atraso no pagamento e custos de cheque especial", "02000000", "Loans and financing"),
        new("02020000", "Interests charged", "Juros cobrados", "02000000", "Loans and financing"),
        new("02030000", "Financing", "Financiamento", "02000000", "Loans and financing"),
        new("02030001", "Real estate financing", "Financiamento imobiliário", "02000000", "Financing"),
        new("02030002", "Vehicle financing", "Financiamento de veículos", "02000000", "Financing"),
        new("02030003", "Student loan", "Empréstimo estudantil", "02000000", "Financing"),
        new("02040000", "Loans", "Empréstimos", "02000000", "Loans and financing"),
        new("03000000", "Investments", "Investimentos", null, null),
        new("03010000", "Automatic investment", "Investimento automático", "03000000", "Investments"),
        new("03020000", "Fixed income", "Renda fixa", "03000000", "Investments"),
        new("03030000", "Mutual funds", "Fundos multimercado", "03000000", "Investments"),
        new("03040000", "Variable income", "Renda variável", "03000000", "Investments"),
        new("03050000", "Margin", "Ajuste de margem", "03000000", "Investments"),
        new("03060000", "Proceeds interests and dividends", "Juros de rendimentos de dividendos", "03000000", "Investments"),
        new("03070000", "Pension", "Pensão", "03000000", "Investments"),
        new("04000000", "Same person transfer", "Transferência mesma titularidade", null, null),
        new("04010000", "Same person transfer - CASH", "Transferência mesma titularidade - Dinheiro", "04000000", "Same person transfer"),
        new("04020000", "Same person transfer - PIX", "Transferência mesma titularidade - PIX", "04000000", "Same person transfer"),
        new("04030000", "Same person transfer - TED", "Transferência mesma titularidade - TED", "04000000", "Same person transfer"),
        new("05000000", "Transfers", "Transferências", null, null),
        new("05010000", "Transfer - Bank Slip", "Transferência - Boleto bancário", "05000000", "Transfers"),
        new("05020000", "Transfer - Cash", "Transferência - Dinheiro", "05000000", "Transfers"),
        new("05030000", "Transfer - Check", "Transferência - Cheque", "05000000", "Transfers"),
        new("05040000", "Transfer - DOC", "Transferências- DOC", "05000000", "Transfers"),
        new("05050000", "Transfer - Foreign Exchange", "Transferência - Câmbio", "05000000", "Transfers"),
        new("05060000", "Transfer - Internal", "Transferência - Mesma instituição", "05000000", "Transfers"),
        new("05070000", "Transfer - PIX", "Transferência - PIX", "05000000", "Transfers"),
        new("05080000", "Transfer - TED", "Transferência - TED", "05000000", "Transfers"),
        new("05090000", "Third party transfers", "Transferências para terceiros", "05000000", "Transfers"),
        new("05090001", "Third party transfer - Bank Slip", "Transferência para terceiros - Boleto bancário", "05090000", "Third party transfers"),
        new("05090002", "Third party transfer - Debit Card", "Transferência para terceiros - Débito", "05090000", "Third party transfers"),
        new("05090003", "Third party transfer - DOC", "Transferência para terceiros - DOC", "05090000", "Third party transfers"),
        new("05090004", "Third party transfer - PIX", "Transferência para terceiros - PIX", "05090000", "Third party transfers"),
        new("05090005", "Third party transfer - TED", "Transferência para terceiros - TED", "05090000", "Third party transfers"),
        new("05100000", "Credit card payment", "Pagamento de cartão de crédito", "05000000", "Transfers"),
        new("06000000", "Legal obligations", "Obrigações legais", null, null),
        new("06010000", "Blocked balances", "Saldo bloqueado", "06000000", "Legal obligations"),
        new("06020000", "Alimony", "Pensão alimentícia", "06000000", "Legal obligations"),
        new("07000000", "Services", "Serviços", null, null),
        new("07010000", "Telecommunications", "Telecomunicação", "07000000", "Services"),
        new("07010001", "Internet", "Internet", "07010000", "Telecommunications"),
        new("07010002", "Mobile", "Celular", "07010000", "Telecommunications"),
        new("07010003", "TV", "TV", "07010000", "Telecommunications"),
        new("07020000", "Education", "Educação", "07000000", "Services"),
        new("07020001", "Online Courses", "Cursos online", "07020000", "Education"),
        new("07020002", "University", "Universidade", "07020000", "Education"),
        new("07020003", "School", "Escola", "07020000", "Education"),
        new("07020004", "Kindergarten", "Creche", "07020000", "Education"),
        new("07030000", "Wellness and fitness", "Saúde e bem-estar", "07000000", "Services"),
        new("07030001", "Gyms and fitness centers", "Academia e centros de lazer", "07030000", "Wellness and fitness"),
        new("07030002", "Sports practice", "Prática de esportes", "07030000", "Wellness and fitness"),
        new("07030003", "Wellness", "Bem-estar", "07030000", "Wellness and fitness"),
        new("07040000", "Tickets", "Bilhetes", "07000000", "Services"),
        new("07040001", "Stadiums and arenas", "Estádios e arenas", "07040000", "Tickets"),
        new("07040002", "Landmarks and museums", "Museus e pontos turísticos", "07040000", "Tickets"),
        new("07040003", "Cinema, theater and concerts", "Cinema, Teatro e Concertos", "07040000", "Tickets"),
        new("08000000", "Shopping", "Compras", null, null),
        new("08010000", "Online shopping", "Compras online", "08000000", "Shopping"),
        new("08020000", "Electronics", "Eletrônicos", "08000000", "Shopping"),
        new("08030000", "Pet supplies and vet", "Pet Shops e veterinários", "08000000", "Shopping"),
        new("08040000", "Clothing", "Vestiário", "08000000", "Shopping"),
        new("08050000", "Kids and toys", "Artigos infantis", "08000000", "Shopping"),
        new("08060000", "Bookstore", "Livraria", "08000000", "Shopping"),
        new("08070000", "Sports goods", "Artigos esportivos", "08000000", "Shopping"),
        new("08080000", "Office supplies", "Papelaria", "08000000", "Shopping"),
        new("08090000", "Cashback", "Cashback", "08000000", "Shopping"),
        new("09000000", "Digital services", "Serviços digitais", null, null),
        new("09010000", "Gaming", "Jogos e videogames", "09000000", "Digital services"),
        new("09020000", "Video streaming", "Streaming de vídeo", "09000000", "Digital services"),
        new("09030000", "Music streaming", "Streaming de música", "09000000", "Digital services"),
        new("10000000", "Groceries", "Supermercado", null, null),
        new("11000000", "Food and drinks", "Alimentos e bebidas", null, null),
        new("11010000", "Eating out", "Restaurantes, bares e lanchonetes", "11000000", "Food and drinks"),
        new("11020000", "Food delivery", "Delivery de alimentos", "11000000", "Food and drinks"),
        new("12000000", "Travel", "Viagens", null, null),
        new("12010000", "Airport and airlines", "Aeroportos e cias. aéreas", "12000000", "Travel"),
        new("12020000", "Accomodation", "Hospedagem", "12000000", "Travel"),
        new("12030000", "Mileage programs", "Programas de milhagem", "12000000", "Travel"),
        new("12040000", "Bus tickets", "Passagem de ônibus", "12000000", "Travel"),
        new("13000000", "Donations", "Doações", null, null),
        new("14000000", "Gambling", "Apostas", null, null),
        new("14010000", "Lottery", "Loteria", "14000000", "Gambling"),
        new("14020000", "Online bet", "Apostas online", "14000000", "Gambling"),
        new("15000000", "Taxes", "Impostos", null, null),
        new("15010000", "Income taxes", "Imposto de renda", "15000000", "Taxes"),
        new("15020000", "Taxes on investments", "Imposto sobre investimentos", "15000000", "Taxes"),
        new("15030000", "Tax on financial operations", "Impostos sobre operações financeiras", "15000000", "Taxes"),
        new("16000000", "Bank fees", "Taxas bancárias", null, null),
        new("16010000", "Account fees", "Taxas de conta corrente", "16000000", "Bank fees"),
        new("16020000", "Wire transfer fees and ATM fees", "Taxas sobre transferências e caixa eletrônico", "16000000", "Bank fees"),
        new("16030000", "Credit card fees", "Taxas de cartão de crédito", "16000000", "Bank fees"),
        new("17000000", "Housing", "Moradia", null, null),
        new("17010000", "Rent", "Aluguel", "17000000", "Housing"),
        new("17020000", "Utilities", "Serviços de utilidade pública", "17000000", "Housing"),
        new("17020001", "Water", "Água", "17020000", "Utilities"),
        new("17020002", "Electricity", "Eletricidade", "17020000", "Utilities"),
        new("17020003", "Gas", "Gás", "17020000", "Utilities"),
        new("17030000", "Houseware", "Utensílios para casa", "17000000", "Housing"),
        new("17040000", "Urban land and building tax", "Impostos sobre moradia", "17000000", "Housing"),
        new("18000000", "Healthcare", "Saúde", null, null),
        new("18010000", "Dentist", "Dentista", "18000000", "Healthcare"),
        new("18020000", "Pharmacy", "Farmácia", "18000000", "Healthcare"),
        new("18030000", "Optometry", "Ótica", "18000000", "Healthcare"),
        new("18040000", "Hospital clinics and labs", "Hospitais, clínicas e laboratórios", "18000000", "Healthcare"),
        new("19000000", "Transportation", "Transporte", null, null),
        new("19010000", "Taxi and ride-hailing", "Táxi e transporte privado urbano", "19000000", "Transportation"),
        new("19020000", "Public transportation", "Transporte público", "19000000", "Transportation"),
        new("19030000", "Car rental", "Aluguel de veículos", "19000000", "Transportation"),
        new("19040000", "Bicycle", "Aluguel de bicicletas", "19000000", "Transportation"),
        new("19050000", "Automotive", "Serviços automotivos", "19000000", "Transportation"),
        new("19050001", "Gas stations", "Postos de gasolina", "19050000", "Automotive"),
        new("19050002", "Parking", "Estacionamentos", "19050000", "Automotive"),
        new("19050003", "Tolls and in vehicle payment", "Pedágios e pagamentos no veículo", "19050000", "Automotive"),
        new("19050004", "Vehicle ownership taxes and fees", "Taxas e impostos sobre veículos", "19050000", "Automotive"),
        new("19050005", "Vehicle maintenance", "Manutenção de veículos", "19050000", "Automotive"),
        new("19050006", "Traffic tickets", "Multas de trânsito", "19050000", "Automotive"),
        new("20000000", "Insurance", "Seguros", null, null),
        new("200100000", "Life insurance", "Seguro de vida", "20000000", "Insurance"),
        new("200200000", "Home insurance", "Seguro residencial", "20000000", "Insurance"),
        new("200300000", "Health insurance", "Seguro saúde", "20000000", "Insurance"),
        new("200400000", "Vehicle insurance", "Seguro de veículos", "20000000", "Insurance"),
        new("21000000", "Leisure", "Lazer", null, null),
        new("99999999", "Other", "Outros", null, null)
    ];
}
