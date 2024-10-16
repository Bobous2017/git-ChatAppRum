using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppRum.Model
{
    public class ChatRoom
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime LatestMessageTime { get; set; }
    }
}
