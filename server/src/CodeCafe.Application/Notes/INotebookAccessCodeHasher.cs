namespace CodeCafe.Application.Notes;

public interface INotebookAccessCodeHasher
{
    string Hash(string accessCode);

    bool Verify(string accessCodeHash, string providedCode);
}
