using IbnElgm3a.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IbnElgm3a.Models.Converters
{
    public class EncryptedStringConverter : ValueConverter<string?, string?>
    {
        public EncryptedStringConverter(IAesEncryptionService encryptionService, ConverterMappingHints? mappingHints = null)
            : base(
                v => v == null ? null : encryptionService.Encrypt(v),
                v => v == null ? null : encryptionService.Decrypt(v),
                mappingHints)
        {
        }
    }

    public class EncryptedDecimalConverter : ValueConverter<decimal, string>
    {
        public EncryptedDecimalConverter(IAesEncryptionService encryptionService, ConverterMappingHints? mappingHints = null)
            : base(
                v => encryptionService.EncryptDecimalToString(v),
                v => encryptionService.DecryptStringToDecimal(v),
                mappingHints)
        {
        }
    }
}
