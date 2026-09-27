using Pathly_DTOs;

namespace Pathly_Interfaces.IService
{
    /// <summary>
    /// Next-term prediction for term-structured academic records (secondary school Gr 8-11,
    /// and Gr 12 up to — but not including — a populated final block). Deliberately returns a
    /// direction + band + confidence rather than a false-precision point estimate when few data
    /// points exist.
    /// </summary>
    public interface IAcademicPredictionService
    {
        /// <summary>
        /// Builds a per-subject projection for the next same-year term from the stored term
        /// history. Returns an empty/disabled result when the record does not qualify for
        /// prediction (fewer than two populated terms, a populated final block, or no
        /// term structure).
        /// </summary>
        AcademicPredictionDto PredictNextTerm(ExtractedAcademicRecordDto record);
    }
}
