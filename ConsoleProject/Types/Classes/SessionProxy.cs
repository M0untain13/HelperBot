using ConsoleProject.Types.Delegates;
using ConsoleProject.Types.Enums;

namespace ConsoleProject.Types.Classes;

public class SessionProxy
{
    private Session _session;

    public SessionState State => _session.State;

    public SessionProxy(Session session)
    {
        _session = session;
    }

    public bool Add(Task task, MessageHandle handle) => _session.Add(task, handle);
    public void Start() => _session.Start();
    public void Close() => _session.Close();
}
