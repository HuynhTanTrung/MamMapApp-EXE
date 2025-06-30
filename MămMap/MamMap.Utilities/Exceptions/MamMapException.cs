using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Utilities.Exceptions
{
    public class MamMapException : Exception
    {
        public MamMapException()
        {

        }

        public MamMapException(string message) : base(message)
        {

        }

        public MamMapException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
