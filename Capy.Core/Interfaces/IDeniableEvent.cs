namespace Capy.Core.Interfaces;

public interface IDeniableEvent {
    bool IsAllowed { get; set; }    
}
