using System.Collections.Generic;

namespace PlayWithAsync.Models
{
    public class WebRequestAsyncState
    {
        /// <summary>
        /// Whether this state is completed or not
        /// </summary>
        public bool Completed { get; set; }
        
        /// <summary>
        /// Binary data from response
        /// </summary>
        public List<byte> Data { get; set; }
    }
}