using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PegMatch
{
    class StreamContentLoader : IContentLoader
    {
        private TextReader m_Input;

        public string Name => "stdin";

        public StreamContentLoader(TextReader input)
        {
            m_Input = input;
        }

        public ContentCharData ReadAllChars()
        {
            var data = m_Input.ReadToEnd().ToCharArray();
            return new ContentCharData(data, data.Length);
        }
    }
}
