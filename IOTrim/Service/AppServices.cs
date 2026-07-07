using ClosedXML.Excel;
using IOTrim.Service;
using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;

public class AppServices
{

    public static OpcUaService OpcUaService = new OpcUaService();
    public static void Export_Data(DataGrid dataGrid, string fileName)
    {
        try
        {
            LogService.AddLog("Export button clicked.");

            if (dataGrid.ItemsSource == null)
            {
                LogService.AddLog("Export stopped. No data available in grid.");
                MessageBox.Show("No data available to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataTable dt = null;

            if (dataGrid.ItemsSource is DataView dataView)
            {
                dt = dataView.ToTable();
            }
            else if (dataGrid.ItemsSource is IEnumerable enumerable)
            {
                dt = ConvertIEnumerableToDataTable(enumerable);
            }

            if (dt == null || dt.Rows.Count == 0)
            {
                LogService.AddLog("Export stopped. No rows could be parsed from the grid.");
                MessageBox.Show("No rows available to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string defaultFileName = (!string.IsNullOrEmpty(fileName) ? $"{fileName}_" : "Log_") + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel File (*.xlsx)|*.xlsx",
                FileName = defaultFileName
            };

            bool? dialogResult = saveFileDialog.ShowDialog();

            if (dialogResult != true)
            {
                LogService.AddLog("Export cancelled by user.");
                return;
            }

            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(dt, $"{fileName}");
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(saveFileDialog.FileName);
            }

            LogService.AddLog($"Excel export completed. File:{saveFileDialog.FileName}, Rows:{dt.Rows.Count}");
            MessageBox.Show("Excel exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogService.AddException("Excel export failed", ex);
            MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static DataTable ConvertIEnumerableToDataTable(IEnumerable enumerable)
    {
        DataTable dt = new DataTable();
        IEnumerator enumerator = enumerable.GetEnumerator();

        if (!enumerator.MoveNext()) return dt;

        object firstItem = enumerator.Current;
        if (firstItem == null) return dt;

        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(firstItem.GetType());

        foreach (PropertyDescriptor prop in properties)
        {
            Type propType = prop.PropertyType;
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propType = Nullable.GetUnderlyingType(propType);
            }
            dt.Columns.Add(prop.Name, propType);
        }

        do
        {
            object item = enumerator.Current;
            DataRow row = dt.NewRow();
            foreach (PropertyDescriptor prop in properties)
            {
                row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
            }
            dt.Rows.Add(row);
        } while (enumerator.MoveNext());

        return dt;
    }
}