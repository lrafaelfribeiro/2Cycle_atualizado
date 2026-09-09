using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Core
{
    public static class DateHelper
    {
        /// <summary>
        /// Devolve a segunda-feira da semana a que a data pertence (00:00, sem componente de hora).
        /// </summary>
        public static DateTime GetStartOfWeek(DateTime date)
        {
            var today = date.Date;
            int diffToMonday = ((int)today.DayOfWeek + 6) % 7; // Domingo=0 no .NET, por isso o +6 %7 empurra para segunda-feira
            return today.AddDays(-diffToMonday);
        }
    }
}
