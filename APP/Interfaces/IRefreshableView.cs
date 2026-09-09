using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Interfaces
{
    public interface IRefreshableView
    {
        Task RefreshAsync();
    }
}
