using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BalatroSeedOracle.Helpers
{
    /// <summary>
    /// A TextWriter that filters out duplicate seed report lines from Motely's console output
    /// </summary>
    public class FilteredConsoleWriter : TextWriter
    {
        private readonly TextWriter _originalWriter;
        private readonly Action<string>? _onOutput;
        private readonly bool _filterSeedLines;

        // Regex to match Motely's CSV output format: SEED,SCORE,TALLY1,TALLY2,...
        private static readonly Regex SeedLineRegex = new Regex(
            @"^[A-Z0-9]+,\d+(?:,\d+)*$",
            RegexOptions.Compiled
        );

        public FilteredConsoleWriter(
            TextWriter originalWriter,
            Action<string>? onOutput = null,
            bool filterSeedLines = true
        )
        {
            _originalWriter = originalWriter;
            _onOutput = onOutput;
            _filterSeedLines = filterSeedLines;
        }

        public override Encoding Encoding => _originalWriter.Encoding;

        public override void WriteLine(string? value)
        {
            // ALWAYS send to the callback first (for SearchInstance to process)
            if (value != null)
            {
                _onOutput?.Invoke(value + Environment.NewLine);
            }

            if (_filterSeedLines && value != null)
            {
                // Filter out seed result lines from console display only
                if (SeedLineRegex.IsMatch(value.Trim()))
                {
                    // Skip writing to console - but callback already got it
                    return;
                }
            }

            // Pass through all other output to console
            _originalWriter.WriteLine(value);
        }

        public override void Write(string? value)
        {
            // For partial writes, just pass through
            _originalWriter.Write(value);
            if (value != null)
            {
                _onOutput?.Invoke(value);
            }
        }

        public override void Write(char value)
        {
            _originalWriter.Write(value);
            _onOutput?.Invoke(value.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Don't dispose the original writer - we don't own it
                _originalWriter.Flush();
            }
            base.Dispose(disposing);
        }
    }
}
