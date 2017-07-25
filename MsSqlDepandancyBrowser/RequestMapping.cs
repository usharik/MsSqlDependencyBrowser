using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsSqlDepandancyBrowser
{
    [AttributeUsage(AttributeTargets.Method)]
    class RequestMapping : Attribute
    {
        private string path;
        private string method;

        public RequestMapping(string path, string method)
        {
            this.path = path;
            this.method = method;
        }

        public override string ToString()
        {
            return $"{path} - {method}";
        }
    }
}
