using Business_IntelligenceV2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Windows.Forms;
using Business_IntelligenceV2;

namespace Business_IntelligenceV2
{
    public partial class FormMain : Form
    {
        public string pathName = string.Empty;
        public string selectData;

        public FormMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog arquivoBI = new OpenFileDialog
            {
                InitialDirectory = @"c:\",
                Title = "Selecione o arquivo CSV",
                Filter = "CSV|*.csv"
            };
            if (arquivoBI.ShowDialog() == DialogResult.OK)
            {
                pathName = arquivoBI.FileName;
                selectData = dateTimePicker1.Value.ToString("dd.MM.yyyy");
            }
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(pathName))
            {
                MessageBox.Show("Não foi possível iniciar, arquivo não foi selecionado!");
                return;
            }

            selectData = dateTimePicker1.Value.ToString("dd.MM.yyyy");

            var render = new StreamReader(File.OpenRead(pathName));
            var line = render.ReadLine();
            var columns = line.Split(';');

            (int indexStatusOrigem,
             int indexNumPedidosFilial,
             int indexNumPDV,
             int indexDataOrcamento,
             int indexQuantItem,
             int indexValorTotalItem) = SetColumnsIndex(columns);

            var info = BuildInfoStatus(render, indexStatusOrigem, indexNumPedidosFilial, indexNumPDV, indexDataOrcamento, indexQuantItem, indexValorTotalItem);

            if (!IsDateInFile(info, selectData))
            {
                MessageBox.Show($"A data {selectData} não foi encontrada no arquivo.");
                return;
            }

            // Cálculo dos valores
            CalculateValues(info, indexNumPedidosFilial, indexNumPDV);

            MessageBox.Show("Validação concluída!");
        }

        private static (int, int, int, int, int, int) SetColumnsIndex(string[] columns)
        {
            int indexStatusOrigem = Array.IndexOf(columns, "STATUS_ORIGEM");
            int indexNumPedidosFilial = Array.IndexOf(columns, "NUMERO_PEDIDO_FILIAL");
            int indexNumPDV = Array.IndexOf(columns, "NUMERO_PDV");
            int indexDataOrcamento = Array.IndexOf(columns, "DATA_ORCAMENTO");
            int indexQuantItem = Array.IndexOf(columns, "QUANTIDADE_ITEM");
            int indexValorTotalItem = Array.IndexOf(columns, "VALOR_LIQ_TOTAL_ITEM");

            return (indexStatusOrigem, indexNumPedidosFilial, indexNumPDV, indexDataOrcamento, indexQuantItem, indexValorTotalItem);
        }

        private static List<InfoStatus> BuildInfoStatus(StreamReader reader, int indexStatusOrigem, int indexNumPedidosFilial, int indexNumPDV, int indexDataOrcamento, int indexQuantItem, int indexValorTotalItem)
        {
            string line;
            var info = new List<InfoStatus>();

            while ((line = reader.ReadLine()) != null)
            {
                var values = line.Split(';');
                var infoStatus = new InfoStatus
                {
                    StatusOrigem = indexStatusOrigem != -1 ? values[indexStatusOrigem] : string.Empty,
                    NumPedidosFilial = indexNumPedidosFilial != -1 ? values[indexNumPedidosFilial] : string.Empty,
                    NumPDV = indexNumPDV != -1 ? values[indexNumPDV] : string.Empty,
                    DataOrcamento = indexDataOrcamento != -1 ? values[indexDataOrcamento] : string.Empty,
                    QuantItem = indexQuantItem != -1 ? values[indexQuantItem] : string.Empty,
                    ValorTotalItem = indexValorTotalItem != -1 ? values[indexValorTotalItem] : string.Empty
                };
                info.Add(infoStatus);
            }
            return info;
        }

        private static bool IsDateInFile(List<InfoStatus> info, string dateToCheck)
        {
            return info.Any(i => i.DataOrcamento == dateToCheck && (i.StatusOrigem.Equals("Aprovado", StringComparison.OrdinalIgnoreCase) || i.StatusOrigem.Equals("Rejeitado", StringComparison.OrdinalIgnoreCase)));
        }

        private void CalculateValues(List<InfoStatus> info, int indexNumPedidosFilial, int indexNumPDV)
        {
            double valorTotalPedidos = 0;
            double valorTotalPedidosAprovados = 0;
            double valorTotalPedidosRejeitados = 0;
            double valorTotalItensAprovados = 0;
            double valorTotalItensRejeitados = 0;

            HashSet<string> uniqueNumPedidosFilialAprovados = new HashSet<string>();
            HashSet<string> uniqueNumPedidosFilialRejeitados = new HashSet<string>();
            HashSet<string> uniqueNumPDVAprovados = new HashSet<string>();
            HashSet<string> uniqueNumPDVRejeitados = new HashSet<string>();


            foreach (var infos in info)
            {
                if (infos.DataOrcamento == selectData)
                {
                    double valorTotalItem = double.Parse(infos.ValorTotalItem);

                    if (infos.StatusOrigem.Equals("Aprovado", StringComparison.OrdinalIgnoreCase))
                    {
                        valorTotalPedidos += valorTotalItem;
                        valorTotalPedidosAprovados += valorTotalItem;
                        valorTotalItensAprovados += double.Parse(infos.QuantItem);
                        uniqueNumPedidosFilialAprovados.Add(infos.NumPedidosFilial);
                        uniqueNumPDVAprovados.Add(infos.NumPDV);
                    }
                    else if (infos.StatusOrigem.Equals("Rejeitado", StringComparison.OrdinalIgnoreCase))
                    {
                        valorTotalPedidos += valorTotalItem;
                        valorTotalPedidosRejeitados += valorTotalItem;
                        valorTotalItensRejeitados += double.Parse(infos.QuantItem);
                        uniqueNumPedidosFilialRejeitados.Add(infos.NumPedidosFilial);
                        uniqueNumPDVRejeitados.Add(infos.NumPDV);
                    }
                }
            }

            int somaUniquePedidos = uniqueNumPedidosFilialAprovados.Count + uniqueNumPedidosFilialRejeitados.Count;
            double somaQuantidadeItens = valorTotalItensAprovados + valorTotalItensRejeitados;
            // Atualizar labels
            labelValorPedidos.Text = valorTotalPedidos.ToString();
            labelValorAprovados.Text = valorTotalPedidosAprovados.ToString();
            labelValorAprovadosCp.Text = valorTotalPedidosAprovados.ToString();
            labelPedidosRejeitados.Text = valorTotalPedidosRejeitados.ToString();
            labelPedidosRejeitadosCp.Text = valorTotalPedidosRejeitados.ToString();
            labelItensAprovados.Text = valorTotalItensAprovados.ToString();
            labelItensRejeitados.Text = valorTotalItensRejeitados.ToString();
            labelNumPedidosTotal.Text = somaUniquePedidos.ToString();
            labelQuantidadeItens.Text = somaQuantidadeItens.ToString();


            // Contagem de valores únicos
            labelUniqueNumPedidosFilialAprovados.Text = uniqueNumPedidosFilialAprovados.Count.ToString();
            labelUniqueNumPedidosFilialRejeitados.Text = uniqueNumPedidosFilialRejeitados.Count.ToString();
            // labelUniqueNumPDVAprovados.Text = uniqueNumPDVAprovados.Count.ToString();
           // labelUniqueNumPDVRejeitados.Text = uniqueNumPDVRejeitados.Count.ToString();

            
        }
    }
}