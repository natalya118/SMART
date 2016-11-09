using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Smart.Models
{
    public static class EncryptPassword
    {
        //MD5 Encryption Algorithm
        public static string GetEncryptedPassword(string password)
        {
            var md5 = new MD5CryptoServiceProvider();
            md5.ComputeHash(Encoding.ASCII.GetBytes(password));
            var result = md5.Hash;

            var stringBuilder = new StringBuilder();
            for (int i = 1; i < result.Length; i++)
                stringBuilder.Append(result[i].ToString("x2"));

            return stringBuilder.ToString();
        }
    }
}