using System.Text;

namespace Sip2Form
{
    public static class Sip2Extensions
    {
        public static string WithCheckSum(this string message)
        {
            var chkSum = ComputeChecksum(message);
            return $"{message}{chkSum}{"\r"}";
        }

        private static string ComputeChecksum(string dataToCalculate)
        {
            var byteToCalculate = Encoding.ASCII.GetBytes(dataToCalculate);
            var checksum = 0;
            foreach (var chData in byteToCalculate)
            {
                checksum += chData;
            }
            checksum = -checksum & 0xFFFF;
            return checksum.ToString("X4");
        }
    }
}
