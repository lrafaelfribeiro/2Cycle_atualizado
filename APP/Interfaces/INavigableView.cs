using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Interfaces
{
    public interface INavigableView
    {
        event EventHandler<string> TabRequested;
    }
}
