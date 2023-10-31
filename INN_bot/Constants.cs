namespace CRMPTelegramBotINN
{
    internal static class Constants
    {
        internal static string StartMessage => "Приветствую! Для поиска организаций по ИНН введите /inn. Чтобы узнать о всех возможностях бота, введите /help";
        internal static string HelpMessage => "/start - начать общение с ботом.\n" +
            "/help - вывести справку о доступных командах.\n" +
            "/hello - вывести имя, фамилию, email создателя и дату получения задания о создании.\n" +
            "/inn - получить наименования и адреса компаний по ИНН " +
            "(можно указать несколько ИНН, разделенных переводом строки, в обном сообщении).\n" +
            "/full - получить подробную информацию о компании по ИНН (только одна компания за раз).\n" +
            "/egrul - получить pdf-файл с выпиской из ЮГРЮЛ.";
        internal static string HelloMessage => "Ахмадеев Давид / ahmadeevd@gmail.com / 30.10.2023";
        internal static string InnMessage => "Введите ИНН (можно ввести несколько ИНН: каждый на новой строке) или нажмите \"Назад\" для выхода из режима поиска.";
        internal static string BackMessage => "Выход из режима поиска.";
        internal static string FullMessage => "Введите ИНН, выберите один из последних или нажмите \"Назад\" для выхода из режима поиска.";
        internal static string UnknownMessage => "Введенная команда не была распознана. Для вывода помощи введти /help";
        internal static string EmptyMessage(string arg = "") => $"По запросу {arg} ничего не было найдено";
        internal static string WrongMessage(string arg = "") => $"Ошибка в запросе: {arg}\n ИНН должен состоять из 10 арабских цифр.";
    }
}
