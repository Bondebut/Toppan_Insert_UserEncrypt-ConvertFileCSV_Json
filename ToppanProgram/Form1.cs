using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Security.Cryptography;


namespace ToppanProgram
{
    public partial class Form1 : Form
    {
        private readonly SqlConnection conn = new SqlConnection(@"Data Source=BON\SQLEXPRESS;Initial Catalog=user_login;Integrated Security=True;TrustServerCertificate=True");
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = txbUser.Text;
            string password = txbPass.Text;

            if (password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters long.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Generate salt
            byte[] salt = GenerateSalt();

            // Hash password with salt
            string hashedPassword = HashPassword(password, salt);

            try
            {
                conn.Open();

                string query = "INSERT INTO Loginapp (username, password_hash, salt) VALUES (@username, @passwordHash, @salt)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@passwordHash", hashedPassword);
                cmd.Parameters.AddWithValue("@salt", Convert.ToBase64String(salt));

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    MessageBox.Show("User registered successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);


                }
                else
                {
                    MessageBox.Show("Failed to register user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                conn.Close();
            }
            GetEmpList();
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[16]; // 16 bytes = 128 bits
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private string HashPassword(string password, byte[] salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] saltedPassword = new byte[passwordBytes.Length + salt.Length];
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Buffer.BlockCopy(salt, 0, saltedPassword, passwordBytes.Length, salt.Length);

                byte[] hashedBytes = sha256.ComputeHash(saltedPassword);
                return Convert.ToBase64String(hashedBytes);
            }
    

        }
        void GetEmpList()
        {

            string query = "SELECT username FROM Loginapp";
            SqlCommand c = new SqlCommand(query, conn);
            SqlDataAdapter sd = new SqlDataAdapter(c);
            DataTable dt = new DataTable();
            sd.Fill(dt);
            dataGridView1.DataSource = dt;


        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void shPass_CheckedChanged(object sender, EventArgs e)
        {
            txbPass.PasswordChar = shPass.Checked ? '\0':'*';
        }

        private void txbUser_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                conn.Open();
                string username = txbUser.Text;
                string query = "DELETE FROM Loginapp WHERE username = @username";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    MessageBox.Show("Successfully Deleted: " + username);
                }
                else
                {
                    MessageBox.Show("No user found with the username: " + username);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                conn.Close();
            }
            GetEmpList();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetEmpList();
        }

        private void btnData_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form2 form = new Form2();
            form.ShowDialog();
        }
    }
}
