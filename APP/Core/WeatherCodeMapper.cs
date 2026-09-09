namespace APP.Core
{
    public static class WeatherCodeMapper
    {
        public static (string Emoji, string Description) Map(int code) => code switch
        {
            0 => ("☀️", "Céu limpo"),
            1 => ("🌤️", "Pouco nublado"),
            2 => ("⛅", "Parcialmente nublado"),
            3 => ("☁️", "Nublado"),
            45 or 48 => ("🌫️", "Nevoeiro"),
            51 or 53 or 55 => ("🌦️", "Chuvisco"),
            56 or 57 => ("🌧️", "Chuvisco gelado"),
            61 or 63 or 65 => ("🌧️", "Chuva"),
            66 or 67 => ("🌧️", "Chuva gelada"),
            71 or 73 or 75 => ("🌨️", "Neve"),
            77 => ("🌨️", "Grãos de neve"),
            80 or 81 or 82 => ("🌦️", "Aguaceiros"),
            85 or 86 => ("🌨️", "Aguaceiros de neve"),
            95 => ("⛈️", "Trovoada"),
            96 or 99 => ("⛈️", "Trovoada com granizo"),
            _ => ("🌡️", "Desconhecido")
        };
    }
}
