using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatQueue.Domain.Exceptions
{
    public class QueueFullException : Exception
    {
        public QueueFullException(string message) : base(message)
        {
        }
    }
}
