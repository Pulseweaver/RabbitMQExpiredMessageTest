using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQWrapper
{
    public class TestMessage
    {
        public DateTime MessageCreated = DateTime.Now;
        public bool IsSettingTrue = false;
    }
}
