using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"Versione corrente: {version}");

        var conf = new Configuration();
        var logger = new SimpleLogger(conf.LogFile);
        var crumbs = new Crumbs(conf, logger);

        crumbs.ProgressChanged += (s, e) =>
        {
            Console.Write($"\r{e.Operation}: {e.Processed:N0}/{e.Total:N0}");
        };

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("\nRichiesta di annullamento ricevuta (CTRL+C)...");
            e.Cancel = true; // impedisce la terminazione immediata del processo
            cts.Cancel();    // segnala la cancellazione
        };

        string inputPath = args[0];
        Console.WriteLine($"Selected Path: {inputPath}");

        try
        {
            // esegui su thread pool per non bloccare il main thread UI/console
            await Task.Run(() => crumbs.Run(inputPath, token), token);
            Console.WriteLine("\nOperazione completata con successo.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nOperazione annullata dall'utente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nErrore imprevisto: {ex.Message}");
        }

        Console.WriteLine("Fine programma.");
    }
}
