using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailRepeater
{
    public class MailProcessor
    {
        List<string> keepHeaders;
        Dictionary<string, string> destinationList;
        string sender;

        List<HeaderItem> inputHeaders;
        string inputContent;

        List<string> outgoingEmailList;

        public bool HasRecipients
        {
            get
            {
                return outgoingEmailList.Count > 0;
            }
        }

        public MailProcessor(List<string> keepHeaders, Dictionary<string, string> destinationList, string sender)
        {
            this.keepHeaders = keepHeaders;
            this.destinationList = destinationList;
            this.sender = sender;
        }

        public string Process(List<string> lines)
        {
            ParseInput(lines);
            GetDestinations();
            return BuildEmail();
        }

        void ParseInput(List<string> lines)
        {
            //split up the original into headers and content
            inputHeaders = new List<HeaderItem>();
            bool startContent = false;
            HeaderItem currentItem = new HeaderItem();

            for (int counter = 0; counter < lines.Count; counter ++)
            {
                if (!startContent)   //still in header section?
                {
                    if (string.IsNullOrEmpty(lines[counter].Trim()))
                    {
                        startContent = true;        //indicate that header rows have ended

                        if (!string.IsNullOrEmpty(currentItem.Name))
                            inputHeaders.Add(currentItem.Copy());
                    }
                    else
                    {
                        if (lines[counter].StartsWith(" ") || lines[counter].StartsWith("\t"))
                            currentItem.Lines.Add(lines[counter].Trim());   //add line to existing header record
                        else
                        {
                            if (!string.IsNullOrEmpty(currentItem.Name))
                            {
                                inputHeaders.Add(currentItem.Copy());
                                currentItem = new HeaderItem();
                            }

                            if (lines[counter].Contains(':'))       //split to get the name of the header, and first line value
                            {
                                currentItem.Name = lines[counter].Split(':')[0].Trim();

                                string line = lines[counter] + " ";
                                line = line.Substring(line.IndexOf(':') + 1).Trim();

                                currentItem.Lines.Add(line);
                            }
                        }
                    }
                }
                else
                    inputContent += lines[counter] + "\r\n";
            }
        }

        void GetDestinations()
        {
            //get a list of destinations from the original email
            string originalDestStr = "";

            foreach (string line in inputHeaders.Where(a => a.Name.ToLower() == "to").Select(a => a.Unfold()))
                originalDestStr += " " + line;

            foreach (string line in inputHeaders.Where(a => a.Name.ToLower() == "cc").Select(a => a.Unfold()))
                originalDestStr += " " + line;

            foreach (string line in inputHeaders.Where(a => a.Name.ToLower() == "bcc").Select(a => a.Unfold()))
                originalDestStr += " " + line;

            originalDestStr = originalDestStr.Replace("<", " ").Replace(">", " ").Replace("\"", " ").Replace(",", " ").Replace(";", " ");

            List<string> originalDest = originalDestStr.Split(' ').Where(a => a.Contains("@")).Select(a => a.ToLower().Trim()).ToList();
            outgoingEmailList = new List<string>();

            //try to map each of the original destinations
            foreach (string original in originalDest)
            {
                //1. Is there an exact match?
                if (destinationList.ContainsKey(original))
                {
                    if (!string.IsNullOrEmpty(destinationList[original]))
                        outgoingEmailList.Add(destinationList[original]);
                }
                else
                {
                    //2. Can we match on domain?
                    string matchDomain = "*" + original.Substring(original.IndexOf("@"));

                    if (destinationList.ContainsKey(matchDomain))
                    {
                        if (!string.IsNullOrEmpty(destinationList[matchDomain]))
                            outgoingEmailList.Add(destinationList[matchDomain]);
                    }
                    else
                    {
                        //3. If all else fails, use the global match
                        if (!string.IsNullOrEmpty(destinationList["*"]))
                            outgoingEmailList.Add(destinationList["*"]);
                    }
                }
            }

            outgoingEmailList = outgoingEmailList.Distinct().ToList();
        }

        string BuildEmail()
        {
            //Build the headers
            List<HeaderItem> outputHeaders = new List<HeaderItem>();

            foreach (HeaderItem item in inputHeaders)
                if (keepHeaders.Contains(item.Name.ToLower()))
                    outputHeaders.Add(item);

            HeaderItem fromHeader = new HeaderItem();
            fromHeader.Name = "From";
            fromHeader.Lines.Add(sender);
            outputHeaders.Add(fromHeader);

            string originalTo = "";
            foreach (HeaderItem item in inputHeaders)
                if (item.Name.ToLower().Equals("to"))
                    originalTo = item.Unfold();

            HeaderItem originalToHeader = new HeaderItem();
            originalToHeader.Name = "X-Original-To";
            originalToHeader.Lines.Add(originalTo);
            outputHeaders.Add(originalToHeader);

            string replyTo = "";
            foreach (HeaderItem item in inputHeaders)
                if (item.Name.ToLower().Equals("from"))
                    replyTo = item.Unfold();

            HeaderItem replyToHeader = new HeaderItem();
            replyToHeader.Name = "Reply-To";
            replyToHeader.Lines.Add(replyTo);
            outputHeaders.Add(replyToHeader);

            HeaderItem toHeader = new HeaderItem();
            toHeader.Name = "To";

            for (int counter = 0; counter < outgoingEmailList.Count; counter ++)
            {
                if (counter < outgoingEmailList.Count - 1)
                    toHeader.Lines.Add(outgoingEmailList[counter] + ", ");
                else
                    toHeader.Lines.Add(outgoingEmailList[counter]);
            }

            outputHeaders.Add(toHeader);

            //Add the headers together
            string output = "";

            foreach (HeaderItem item in outputHeaders)
            {
                output += item.Name + ": ";

                if (item.Lines.Count > 0)
                    output += item.Lines[0] + "\r\n";

                if (item.Lines.Count > 1)
                    for (int counter = 1; counter < item.Lines.Count; counter++)
                        output += "    " + item.Lines[counter] + "\r\n";
            }

            output += "\r\n";
            output += inputContent;

            return output;
        }

    }
}
