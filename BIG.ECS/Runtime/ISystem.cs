namespace BIG.ECS
{
    /// <summary>
    /// ECS system interface.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// This initialization is used by <see cref="World"/>.
        /// </summary>
        /// <param name="entityManager">Entity manager belongs to the <see cref="World"/> and it's provided by it into the systems.</param>
        void Initialize(EntityManager entityManager);

        /// <summary>
        /// Called by Update method of <see cref="World"/>.
        /// This is where the system should do its work. The delta time is provided by the <see cref="World"/> and it's the time passed since the last update, which can be used for time-based calculations.
        /// </summary>
        /// <param name="world">World that updates this system.</param>
        /// <param name="deltaTime">Time passed since the last update.</param>
        void Update(World world, in float deltaTime);
    }
}
