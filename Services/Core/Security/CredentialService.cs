using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace GestLog.Services.Core.Security
{
    /// <summary>
    /// Interfaz para el manejo seguro de credenciales
    /// </summary>
    public interface ICredentialService
    {
        /// <summary>
        /// Guarda credenciales de forma segura
        /// </summary>
        bool SaveCredentials(string target, string username, string password);
        
        /// <summary>
        /// Recupera credenciales guardadas
        /// </summary>
        (string username, string password) GetCredentials(string target);
        
        /// <summary>
        /// Elimina credenciales guardadas
        /// </summary>
        bool DeleteCredentials(string target);
        
        /// <summary>
        /// Verifica si existen credenciales para el target especificado
        /// </summary>
        bool CredentialsExist(string target);
    }

    /// <summary>
    /// Servicio para manejo seguro de credenciales usando Windows Credential Manager
    /// </summary>
    public class WindowsCredentialService : ICredentialService
    {
        private const string TARGET_PREFIX = "GestLog_SMTP_";

        public bool SaveCredentials(string target, string username, string password)
        {
            try
            {                var credential = new CREDENTIAL
                {
                    TargetName = TARGET_PREFIX + target,
                    UserName = username,
                    CredentialBlobBytes = Encoding.UTF8.GetBytes(password),
                    CredentialBlobSize = (uint)Encoding.UTF8.GetByteCount(password),
                    Type = CREDENTIAL_TYPE.GENERIC,
                    Persist = CREDENTIAL_PERSIST.LOCAL_MACHINE
                };

                return CredWrite(ref credential, 0);
            }
            catch
            {
                return false;
            }
        }

        public (string username, string password) GetCredentials(string target)
        {
            try
            {                if (CredRead(TARGET_PREFIX + target, CREDENTIAL_TYPE.GENERIC, 0, out IntPtr credentialPtr))
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                    var password = Encoding.UTF8.GetString(credential.CredentialBlobBytes);
                    
                    CredFree(credentialPtr);
                    
                    return (credential.UserName, password);
                }
            }
            catch
            {
                // Ignorar errores y devolver valores vacíos
            }

            return (string.Empty, string.Empty);
        }

        public bool DeleteCredentials(string target)
        {
            try
            {
                return CredDelete(TARGET_PREFIX + target, CREDENTIAL_TYPE.GENERIC, 0);
            }
            catch
            {
                return false;
            }
        }

        public bool CredentialsExist(string target)
        {
            try
            {
                if (CredRead(TARGET_PREFIX + target, CREDENTIAL_TYPE.GENERIC, 0, out IntPtr credentialPtr))
                {
                    CredFree(credentialPtr);
                    return true;
                }
            }
            catch
            {
                // Ignorar errores
            }

            return false;
        }

        #region Windows Credential Manager API

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite([In] ref CREDENTIAL userCredential, [In] uint flags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, CREDENTIAL_TYPE type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, CREDENTIAL_TYPE type, int reservedFlag);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr cred);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public CREDENTIAL_TYPE Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CREDENTIAL_PERSIST Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;

            // Helper property to get credential blob as byte array
            public byte[] CredentialBlobBytes
            {
                get
                {
                    if (CredentialBlobSize == 0) return new byte[0];
                    
                    byte[] credentialBlob = new byte[CredentialBlobSize];
                    Marshal.Copy(CredentialBlob, credentialBlob, 0, (int)CredentialBlobSize);
                    return credentialBlob;
                }
                set
                {
                    if (value != null)
                    {
                        CredentialBlobSize = (uint)value.Length;
                        CredentialBlob = Marshal.AllocHGlobal(value.Length);
                        Marshal.Copy(value, 0, CredentialBlob, value.Length);
                    }
                    else
                    {
                        CredentialBlobSize = 0;
                        CredentialBlob = IntPtr.Zero;
                    }
                }
            }
        }

        private enum CREDENTIAL_TYPE : uint
        {
            GENERIC = 1,
            DOMAIN_PASSWORD = 2,
            DOMAIN_CERTIFICATE = 3,
            DOMAIN_VISIBLE_PASSWORD = 4,
            GENERIC_CERTIFICATE = 5,
            DOMAIN_EXTENDED = 6,
            MAXIMUM = 7,
            MAXIMUM_EX = (MAXIMUM + 1000)
        }

        private enum CREDENTIAL_PERSIST : uint
        {
            SESSION = 1,
            LOCAL_MACHINE = 2,
            ENTERPRISE = 3
        }

        #endregion
    }
}
