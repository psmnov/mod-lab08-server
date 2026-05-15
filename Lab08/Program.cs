using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ScottPlot;

namespace Lab08
{
    struct SimulationData
    {
        public string resultDir;
        public int N;
        public double mu;
        public int serverTime;
        public double[] lambdaData;

        public double[] experimentP0;
        public double[] experimentPn;
        public double[] experimentQ;
        public double[] experimentA;
        public double[] experimentK;

        public double[] theoryP0;
        public double[] theoryPn;
        public double[] theoryQ;
        public double[] theoryA;
        public double[] theoryK;
    }

    class Program
    {
        const int N = 5;
        const int SERVER_TIME = 500;
        const double mu = 1000.0 / SERVER_TIME;
        const int numOfRequests = 100;
        const int SAMPLE_INTERVAL_MS = 20; // интервал опроса для P0

        static string projectDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        static string solutionDir = Directory.GetParent(projectDir).FullName;
        static string resultDir = Path.Combine(solutionDir, "result");

        static void Main()
        {
            Directory.CreateDirectory(resultDir);

            int numOfPoints = 0;
            for (int ms = 600; ms >= 50; ms -= 50) numOfPoints++;

            SimulationData data = new SimulationData();
            data.resultDir = resultDir;
            data.N = N;
            data.mu = mu;
            data.serverTime = SERVER_TIME;
            data.lambdaData = new double[numOfPoints];
            data.experimentA = new double[numOfPoints];
            data.experimentK = new double[numOfPoints];
            data.experimentP0 = new double[numOfPoints];
            data.experimentPn = new double[numOfPoints];
            data.experimentQ = new double[numOfPoints];
            data.theoryA = new double[numOfPoints];
            data.theoryK = new double[numOfPoints];
            data.theoryP0 = new double[numOfPoints];
            data.theoryPn = new double[numOfPoints];
            data.theoryQ = new double[numOfPoints];

            Server server = new Server(N, SERVER_TIME);
            Client client = new Client(server);

            int index = 0;
            for (int ms = 600; ms >= 50; ms -= 50)
            {
                server.reset();

                double lambda = 1000.0 / ms;

               
                CancellationTokenSource cts = new CancellationTokenSource();
                server.startSampling(SAMPLE_INTERVAL_MS, cts.Token);

                for (int id = 1; id <= numOfRequests; id++)
                {
                    client.send(id);
                    Thread.Sleep(ms);
                }
                server.allThreadsEnded();

                
                cts.Cancel();
                Thread.Sleep(SAMPLE_INTERVAL_MS * 2); 

              
                double experimentp0 = (double)server.idleSamples / server.sampleCount;
                double experimentpn = (double)server.rejectedCount / server.requestCount;
                double experimentq = (double)server.processedCount / server.requestCount;
                double experimenta = lambda * experimentq;
                double experimentk = (double)server.sumOfBusyChanels / server.requestCount;

                double rho = lambda / mu;
                double sum = 1.0;
                for (int i = 1; i <= N; i++)
                    sum += Math.Pow(rho, i) / Factorial(i);

                double theoryp0 = 1.0 / sum;
                double theorypn = (Math.Pow(rho, N) / Factorial(N)) * theoryp0;
                double theoryq = 1.0 - theorypn;
                double theorya = lambda * theoryq;
                double theoryk = theorya / mu;

                data.lambdaData[index] = lambda;
                data.experimentP0[index] = experimentp0;
                data.experimentPn[index] = experimentpn;
                data.experimentQ[index] = experimentq;
                data.experimentA[index] = experimenta;
                data.experimentK[index] = experimentk;
                data.theoryP0[index] = theoryp0;
                data.theoryPn[index] = theorypn;
                data.theoryQ[index] = theoryq;
                data.theoryA[index] = theorya;
                data.theoryK[index] = theoryk;

                Console.WriteLine(
                    "lambda={0:F2}  rho={1:F2}  expPn={2:F4}  thPn={3:F4}  expP0={4:F4}  thP0={5:F4}",
                    lambda, rho, experimentpn, theorypn, experimentp0, theoryp0);

                index++;
            }

            SaveTxt(data);

            SaveChart(data, "p-1.png", "Вероятность простоя P0", "P0", data.experimentP0, data.theoryP0);
            SaveChart(data, "p-2.png", "Вероятность отказа Pn", "Pn", data.experimentPn, data.theoryPn);
            SaveChart(data, "p-3.png", "Относительная пропускная способность q", "q", data.experimentQ, data.theoryQ);
            SaveChart(data, "p-4.png", "Абсолютная пропускная способность A (заявок/с)", "A", data.experimentA, data.theoryA);
            SaveChart(data, "p-5.png", "Среднее число занятых каналов", "k", data.experimentK, data.theoryK);
        }

        public static long Factorial(int n)
        {
            if (n <= 1) return 1;
            return n * Factorial(n - 1);
        }

        static void SaveTxt(SimulationData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("СМО с отказами M/M/" + data.N + "/" + data.N);
            sb.AppendLine("n=" + data.N + "  mu=" + data.mu + " заявок/с  serviceTime=" + data.serverTime + " мс");
            sb.AppendLine();
            sb.AppendLine(string.Format(
                "{0,8} {1,7} {2,13} {3,13} {4,13} {5,13} {6,12} {7,12} {8,12} {9,12} {10,12} {11,12}",
                "lambda", "rho",
                "expP0", "thP0",
                "expPn", "thPn",
                "expQ", "thQ",
                "expA", "thA",
                "expK", "thK"));
            sb.AppendLine(new string('-', 140));

            for (int i = 0; i < data.lambdaData.Length; i++)
            {
                double rho = data.lambdaData[i] / data.mu;
                sb.AppendLine(string.Format(
                    "{0,8:F4} {1,7:F4} {2,13:F6} {3,13:F6} {4,13:F6} {5,13:F6} {6,12:F6} {7,12:F6} {8,12:F4} {9,12:F4} {10,12:F4} {11,12:F4}",
                    data.lambdaData[i], rho,
                    data.experimentP0[i], data.theoryP0[i],
                    data.experimentPn[i], data.theoryPn[i],
                    data.experimentQ[i], data.theoryQ[i],
                    data.experimentA[i], data.theoryA[i],
                    data.experimentK[i], data.theoryK[i]));
            }

            string path = Path.Combine(data.resultDir, "data.txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Console.WriteLine("Данные  -> " + path);
        }

        static void SaveChart(SimulationData data, string fileName,
                              string title, string yLabel,
                              double[] experimentY, double[] theoryY)
        {
            Plot plot = new Plot();

            var thLine = plot.Add.Scatter(data.lambdaData, theoryY);
            thLine.Label = "Теория";
            thLine.Color = ScottPlot.Color.FromHex("#2563EB");
            thLine.LineWidth = 2;
            thLine.MarkerSize = 6;

            var expLine = plot.Add.Scatter(data.lambdaData, experimentY);
            expLine.Label = "Эксперимент";
            expLine.Color = ScottPlot.Color.FromHex("#DC2626");
            expLine.LineWidth = 2;
            expLine.MarkerSize = 6;
            expLine.LinePattern = LinePattern.Dashed;

            plot.Title(title + "\nn=" + data.N + ", mu=" + data.mu + " заявок/с");
            plot.XLabel("Интенсивность входного потока lambda (заявок/с)");
            plot.YLabel(yLabel);
            plot.ShowLegend();

            string fullPath = Path.Combine(data.resultDir, fileName);
            plot.SavePng(fullPath, 900, 550);
            Console.WriteLine("График  -> " + fullPath);
        }
    }

    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
    }

    class Server
    {
        private PoolRecord[] pool;
        private object threadLock = new object();

        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;
        public int sumOfBusyChanels = 0;


        public int sampleCount = 0;
        public int idleSamples = 0;

        private int serviceTimeMs;

        public Server(int channels, int serviceTimeMs)
        {
            pool = new PoolRecord[channels];
            this.serviceTimeMs = serviceTimeMs;
        }


        public void startSampling(int intervalMs, CancellationToken token)
        {
            Thread sampler = new Thread(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(intervalMs);
                    lock (threadLock)
                    {
                        int busy = 0;
                        for (int i = 0; i < pool.Length; i++)
                            if (pool[i].in_use) busy++;
                        sampleCount++;
                        if (busy == 0)
                            idleSamples++;
                    }
                }
            });
            sampler.IsBackground = true;
            sampler.Start();
        }

        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                requestCount++;

                int busy = 0;
                for (int i = 0; i < pool.Length; i++)
                    if (pool[i].in_use) busy++;

                sumOfBusyChanels += busy;

                for (int i = 0; i < pool.Length; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        return;
                    }
                }

                rejectedCount++;
            }
        }

        public void Answer(object arg)
        {
            int id = (int)arg;
            Thread.Sleep(serviceTimeMs);

            lock (threadLock)
            {
                for (int i = 0; i < pool.Length; i++)
                    if (pool[i].thread == Thread.CurrentThread)
                    {
                        pool[i].in_use = false;
                        break;
                    }
            }
        }

        public void allThreadsEnded()
        {
            while (true)
            {
                List<Thread> threads = new List<Thread>();
                lock (threadLock)
                {
                    for (int i = 0; i < pool.Length; i++)
                        if (pool[i].in_use && pool[i].thread != null)
                            threads.Add(pool[i].thread);
                }
                if (threads.Count == 0) break;
                foreach (Thread thread in threads) thread.Join();
            }
        }

        public void reset()
        {
            lock (threadLock)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    pool[i].in_use = false;
                    pool[i].thread = null;
                }
                requestCount = 0;
                processedCount = 0;
                rejectedCount = 0;
                sumOfBusyChanels = 0;
                sampleCount = 0;
                idleSamples = 0;
            }
        }
    }

    class Client
    {
        private Server server;
        public event EventHandler<procEventArgs> request;

        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
        }

        public void send(int id)
        {
            procEventArgs args = new procEventArgs();
            args.id = id;
            OnProc(args);
        }

        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
                handler(this, e);
        }
    }

    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
}