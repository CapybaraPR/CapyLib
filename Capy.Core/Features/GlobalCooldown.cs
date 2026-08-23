namespace Capy.Core.Features;

public class GlobalCooldown : IDisposable
{
    public static HashSet<GlobalCooldown> Cooldowns { get; } = new();

    public object Owner { get; private set; }
    public TimeSpan CooldownTime { get; private set; }

    private DateTime _lastUse = DateTime.UtcNow;

    public GlobalCooldown(object owner, TimeSpan cooldownTime)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        CooldownTime = cooldownTime;

        lock (Cooldowns)
        {
            Cooldowns.Add(this);
        }
    }

    public void Use(bool overrideCooldown = false)
    {
        if (overrideCooldown)
        {
            _lastUse = DateTime.UtcNow;
            return;
        }

        if (!Check())
            return;

        _lastUse = DateTime.UtcNow;
    }

    public double GetRemaining() => (DateTime.UtcNow - (_lastUse + CooldownTime)).TotalSeconds;

    public bool Check() => DateTime.UtcNow > _lastUse + CooldownTime;

    public static GlobalCooldown? Get(object owner)
    {
        lock (Cooldowns)
        {
            return Cooldowns.FirstOrDefault(x => Equals(x.Owner, owner));
        }
    }

    public static GlobalCooldown GetOrAdd(object owner, TimeSpan cooldownTime)
    {
        lock (Cooldowns)
        {
            return Cooldowns.FirstOrDefault(x => Equals(x.Owner, owner)) ?? new GlobalCooldown(owner, cooldownTime);
        }
    }

    public static bool TryGet(object owner, out GlobalCooldown? cooldown)
    {
        cooldown = Get(owner);
        return cooldown != null;
    }

    public static HashSet<GlobalCooldown>? Get(TimeSpan cooldown)
    {
        lock (Cooldowns)
        {
            var matches = Cooldowns.Where(x => x.CooldownTime == cooldown).ToHashSet();
            return matches.Count > 0 ? matches : null;
        }
    }

    public static bool TryGet(TimeSpan cooldown, out HashSet<GlobalCooldown>? cooldowns)
    {
        cooldowns = Get(cooldown);
        return cooldowns != null;
    }

    internal static int RemoveAllOwnedBy(object owner)
    {
        lock (Cooldowns)
        {
            var victims = Cooldowns
                .Where(x => ReferenceEquals(x.Owner, owner) || (x.Owner as string) == (owner as string))
                .ToList();

            foreach (var victim in victims)
                Cooldowns.Remove(victim);

            return victims.Count;
        }
    }

    public void Dispose()
    {
        lock (Cooldowns)
        {
            Cooldowns.Remove(this);
        }
    }
}
