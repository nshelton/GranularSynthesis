namespace NWaves.Filters.Adaptive
{
    /// <summary>
    /// Adaptive filter (Least-Mean-Squares algorithm)
    /// </summary>
    public class LmsFilter : AdaptiveFilter
    {
        /// <summary>
        /// Mu
        /// </summary>
        private readonly float _mu;

        /// <summary>
        /// Leakage
        /// </summary>
        protected readonly float _leakage;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="order"></param>
        /// <param name="mu"></param>
        /// <param name="weights"></param>
        /// <param name="leakage"></param>
        public LmsFilter(int order,
                         float mu = 0.1f,
                         float[] weights = null,
                         float leakage = 0)
            : base(order, weights)
        {
            _mu = mu;
            _leakage = leakage;
        }

        /// <summary>
        /// Process input and desired samples
        /// </summary>
        /// <param name="input"></param>
        /// <param name="desired"></param>
        /// <returns></returns>
        public override float Process(float input, float desired)
        {
            var y = Process(input);

            var e = desired - y;
            
            for (var i = 0; i < _order; i++)
            {
                _w[i] = (1 - _leakage * _mu) * _w[i] + _mu * e * _x[i];
            }

            return y;
        }
    }
}
