using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataParallelismComplete {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        CancellationTokenSource cts = new CancellationTokenSource();

        string loopStatus = "non";

        private async void parallelAsyncExecute_Click(object sender, RoutedEventArgs e) {
            resultsWindow.Text = "";

            var progress = new Progress<ProgressModel>();
            progress.ProgressChanged += ReportProgress;

            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions parallelOption = new ParallelOptions {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = System.Environment.ProcessorCount
            };

            var watch = System.Diagnostics.Stopwatch.StartNew();


            try {
                await RunDownloadParallelAsync(progress, parallelOption);
            } catch (OperationCanceledException) {
                this.Dispatcher.Invoke(() => {
                    resultsWindow.Text += $"The download was cancelled : {Environment.NewLine}";
                });

            } finally {
                cts.Dispose();
                cts = new CancellationTokenSource();
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            this.Dispatcher.Invoke(() => {
                resultsWindow.Text += $"{Environment.NewLine} Total execution time: {elapsedMs}";
            });
        }

        private void stopExecute_Click(object sender, RoutedEventArgs e) {
            loopStatus = "stop";
        }

        private void breakExecute_Click(object sender, RoutedEventArgs e) {
            loopStatus = "break";
        }

        private void cancelExecute_Click(object sender, RoutedEventArgs e) {
            cts.Cancel();
        }

        private void ReportProgress(object sender, ProgressModel e) {
            executeProgress.Value = e.PercentageComplete;
        }

        private async Task RunDownloadParallelAsync(IProgress<ProgressModel> progress, ParallelOptions parallelOption) {
            var websites = SiteData();
            int count = 1;


            ProgressModel model = new ProgressModel();

            await Task.Run(() => {
                try {
                    ParallelLoopResult result = Parallel.ForEach<string>(websites, parallelOption, (site, loopState) => {
                        if (parallelOption.CancellationToken.IsCancellationRequested) {
                            this.Dispatcher.Invoke(() => {
                                resultsWindow.Text += $"The download was cancelled : {Environment.NewLine}";
                            });

                            return;
                        }

                        if (loopStatus == "stop") {
                            loopState.Stop();
                            return;
                        } else if (loopStatus == "break") {
                            loopState.Break();
                            return;
                        }

                        var results = DownloadWebsite(site);

                        model.PercentageComplete = (count * 100) / websites.Count;
                        progress.Report(model);
                        count++;

                        this.Dispatcher.Invoke(() => {
                            resultsWindow.Text += $"{results.WebsiteUrl} downloaded: {results.WebsiteData.Length}" +
                               $" characters long. {Environment.NewLine}";
                        });


                    });

                    if (result.IsCompleted) {
                        this.Dispatcher.Invoke(() => {
                            resultsWindow.Text += "Stack ran to completion" + $" {Environment.NewLine}";
                        });

                    } else if (result.IsCompleted == false && result.LowestBreakIteration == null) {
                        //Loop exited prematurely by Stop()
                        this.Dispatcher.Invoke(() => {
                            resultsWindow.Text += "Stack exited by Stop()." + $" {Environment.NewLine}";
                            loopStatus = "non";
                        });
                    } else if (result.IsCompleted == false && result.LowestBreakIteration != null) {
                        //Loop exited by Break()
                        this.Dispatcher.Invoke(() => {
                            resultsWindow.Text += "Stack exited by Break()." + $"{Environment.NewLine}";
                            loopStatus = "non";
                        });
                    }

                } catch (OperationCanceledException) {
                    this.Dispatcher.Invoke(() => {
                        resultsWindow.Text += $"The download was cancelled : {Environment.NewLine}";
                    });

                } finally {
                    cts.Dispose();
                    cts = new CancellationTokenSource();
                }



            });

        }

        public static List<string> SiteData() {
            var webResult = new List<string>();

            webResult.Add("https://www.youtube.com");
            webResult.Add("https://www.google.com");
            webResult.Add("https://www.twitter.com");
            webResult.Add("https://www.cnn.com");
            webResult.Add("https://www.yahoo.com");
            webResult.Add("https://www.facebook.com");
            webResult.Add("https://www.sourceforge.net");
            webResult.Add("https://www.codeproject.com");
            webResult.Add("https://www.stackoverflow.com");
            webResult.Add("https://www.wikipedia.org/wiki/.NET_Framework");
            webResult.Add("https://time.com/4960202/most-influential-websites/");
            webResult.Add("https://www.webopedia.com/TERM/A/application.html");
            webResult.Add("https://ahrefs.com/blog/most-visited-websites/");
            webResult.Add("https://www.google.com/business/website-builder/");
            webResult.Add("https://www.wix.com/");
            webResult.Add("https://99designs.com/blog/web-digital/types-of-websites/");
            webResult.Add("https://blog.hubspot.com/marketing/best-website-designs-list");
            webResult.Add("https://www.britannica.com/technology/software");
            webResult.Add("https://www.sciencedaily.com/terms/computer_software.htm");
            webResult.Add("https://www.computerhope.com/jargon/s/software.htm");

            return webResult;
        }

        private static WebsiteModel DownloadWebsite(string websiteURL) {
            WebsiteModel websiteModel = new WebsiteModel();
            WebClient client = new WebClient();

            websiteModel.WebsiteUrl = websiteURL;
            websiteModel.WebsiteData = client.DownloadString(websiteURL);

            return websiteModel;
        }
    }
}
