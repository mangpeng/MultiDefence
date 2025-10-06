// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("sBqzyVAYzvKVQp3MMkn+08g9Y3UPvT4dDzI5NhW5d7nIMj4+Pjo/PBZmnviNFHigM9r9AivmrKR33WFBEfYA2UgoiKpe5dmwuM0rp09qjUqmTaoydgJaKMJFgmH6kuZWqd28DKDqjccSob5+Q0N9BKyy8geUQuuGJQLLAYXDhEB78ZkyHJTUEU1ufbbQ/b4WWzEyoLIh92Wb+VBwLrO4IHvNj1DPQFQsq276qBdJwu1Mm8Kwv95vLY7LUKGiN/Ybmrt6FkEnsS4kLZVdzKUrgYrlh0cX5NBQrqCzir0+MD8PvT41Pb0+Pj+c5O56Hg9nHK5tXgyU6LCGWYXh1V2+B/dHAGWq2pSY8lb6SEB7UcQ8B8If3LNVG/Fa11NuYPXKZj08Pj8+");
        private static int[] order = new int[] { 3,3,5,4,10,8,11,12,11,11,13,13,12,13,14 };
        private static int key = 63;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
