namespace ADTool.Models;

public record OuNode(string Name, string DistinguishedName, IReadOnlyList<OuNode> Children);
public record AdUser(string UPN, string DisplayName);
