using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerifySign
{
    public class SignatureReference
    {
        public string Name;
        public string Email;
        public string SigText;
    }

    public class SignatureDatabase
    {
        public SignatureReference[] List;
    }
}
