using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailRepeater
{
    public class HeaderItem
    {
        public string Name { get; set; }
        public List<string> Lines { get; set; }

        public HeaderItem()
        {
            Lines = new List<string>();
        }

        public HeaderItem Copy()
        {
            HeaderItem newItem = new HeaderItem();

            newItem.Name = String.Copy(this.Name);
            newItem.Lines = this.Lines.Select(a => String.Copy(a)).ToList();

            return newItem;
        }

        public string Unfold()
        {
            string data = "";

            foreach (string line in Lines)
                data += " " + line;

            return data;
        }
    }
}
