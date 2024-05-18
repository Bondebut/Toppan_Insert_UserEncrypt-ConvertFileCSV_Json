using Microsoft.VisualBasic.FileIO;
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
using System.Text.Json.Serialization;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using Formatting = Newtonsoft.Json.Formatting;


namespace ToppanProgram
{
    public partial class Form2 : Form
    {
        private string connectionString = "Data Source=BON\\SQLEXPRESS;Initial Catalog=user_login;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
        public Form2()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }
    
        private void btnSave_Click_1(object sender, EventArgs e)
        {
            try
            {
                DataTable dtItem = computerlist.DataSource as DataTable;
                if (dtItem == null) return;

                int count = 0;
                foreach (DataRow dr in dtItem.Rows)
                {
                    string username = Convert.ToString(dr["username"]);
                    string department = Convert.ToString(dr["department"]);
                    string license = Convert.ToString(dr["license"]);
                    string installed = Convert.ToString(dr["installed"]);
                    string brand = Convert.ToString(dr["brand"]);
                    string model = Convert.ToString(dr["model"]);
                    string serial = Convert.ToString(dr["serial"]);

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(department) && !string.IsNullOrEmpty(license) &&
                        !string.IsNullOrEmpty(installed) && !string.IsNullOrEmpty(brand) && !string.IsNullOrEmpty(model) &&
                        !string.IsNullOrEmpty(serial))
                    {
                        if (!RecordExists(serial))
                        {
                            InsertRecord(username, department, license, installed, brand, model, serial);
                            count++;
                        }
                        else
                        {
                            MessageBox.Show($"Record with serial '{serial}' already exists. Skipping insertion for this record.", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                if (count > 0)
                {
                    MessageBox.Show($"Items Imported Successfully. Total Imported Records: {count}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    computerlist.DataSource = null;
                    txtFile.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool RecordExists(string serial)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM computerlist WHERE serial = @serial", conn))
                    {
                        cmd.Parameters.AddWithValue("@serial", serial);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Exception: {ex.Message}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void InsertRecord(string username, string department, string license, string installed, string brand, string model, string serial)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO computerlist (username, department, license, installed, brand, model, serial) VALUES (@username, @department, @license, @installed, @brand, @model, @serial)", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@department", department);
                        cmd.Parameters.AddWithValue("@license", license);
                        cmd.Parameters.AddWithValue("@installed", installed);
                        cmd.Parameters.AddWithValue("@brand", brand);
                        cmd.Parameters.AddWithValue("@model", model);
                        cmd.Parameters.AddWithValue("@serial", serial);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Exception: {ex.Message}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    Title = "Select a CSV File"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (!dialog.FileName.EndsWith(".csv"))
                    {
                        MessageBox.Show("Selected file is invalid. Please select a valid CSV file.", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DataTable dtNew = GetDataTableFromCSVFile(dialog.FileName);
                    if (dtNew == null || dtNew.Columns[0].ColumnName.ToLower() != "username")
                    {
                        MessageBox.Show("Invalid Items File", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnSave.Enabled = false;
                        return;
                    }

                    txtFile.Text = dialog.SafeFileName;
                    computerlist.DataSource = dtNew;

                    int importedRecord = 0, invalidItem = 0;

                    foreach (DataGridViewRow row in computerlist.Rows)
                    {
                        if (row.IsNewRow) continue;

                        bool isValidRow = true;
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (string.IsNullOrEmpty(Convert.ToString(cell.Value)))
                            {
                                cell.Style.BackColor = Color.Red;
                                isValidRow = false;
                            }
                        }

                        if (isValidRow)
                        {
                            importedRecord++;
                        }
                        else
                        {
                            invalidItem++;
                        }
                    }

                    if (computerlist.Rows.Count == 0)
                    {
                        btnSave.Enabled = false;
                        MessageBox.Show("There is no data in this file.", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static DataTable GetDataTableFromCSVFile(string csvFilePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    // Read columns from CSV
                    string[] colFields = csvReader.ReadFields();
                    if (colFields != null)
                    {
                        foreach (string column in colFields)
                        {
                            DataColumn dateColumn = new DataColumn(column) { AllowDBNull = true };
                            csvData.Columns.Add(dateColumn);
                        }

                        // Read rows from CSV
                        while (!csvReader.EndOfData)
                        {
                            string[] fieldData = csvReader.ReadFields();
                            if (fieldData != null)
                            {
                                for (int i = 0; i < fieldData.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(fieldData[i]))
                                    {
                                        fieldData[i] = null;
                                    }
                                }
                                csvData.Rows.Add(fieldData);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "GAUTAM POS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return csvData;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON files (*.json)|*.json";
                saveFileDialog.Title = "Export to JSON";
                saveFileDialog.FileName = "exported_data.json";
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    DataTable dt = computerlist.DataSource as DataTable;
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        List<Dictionary<string, object>> data = new List<Dictionary<string, object>>();
                        foreach (DataRow row in dt.Rows)
                        {
                            Dictionary<string, object> rowData = new Dictionary<string, object>();
                            foreach (DataColumn col in dt.Columns)
                            {
                                rowData.Add(col.ColumnName, row[col]);
                            }
                            data.Add(rowData);
                        }
                        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
                        File.WriteAllText(saveFileDialog.FileName, jsonData);
                        MessageBox.Show("Data exported to JSON successfully.", "Export to JSON", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No data available to export.", "Export to JSON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data to JSON: {ex.Message}", "Export to JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void computerlist_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void btnAdmin_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 form = new Form1();
            form.ShowDialog();
        }
    }
}
