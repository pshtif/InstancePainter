/*
 *	Created by:  Peter @sHTiF Stefcek
 */

namespace InstancePainter
{
    public interface IData
    {
        void Invalidate(bool p_fallback);

        void Dispose();
    }
}