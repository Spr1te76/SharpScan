using Tamir.SharpSsh.Sharp;

namespace Tamir.SharpSsh.jsch
{
    /// <summary>
    /// Summary description for JSchException.
    /// </summary>
    public class JSchException : Exception
    {
        public JSchException()
        {
        }

        public JSchException(string msg) : base(msg)
        {
        }
    }
}