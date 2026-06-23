namespace OpcPlc.Tests.CompanionSpecs.WotCon;

using NUnit.Framework;
using Opc.Ua;

/// <summary>
/// Tests for the WoT-Con (Web of Things Connectivity) companion spec node manager.
/// Verifies the CreateAsset / DeleteAsset mRPCs, the per-asset WoTFile File API
/// (Open/Write/Close/CloseAndUpdate), Thing-Description parsing/persistence, and
/// the resulting OPC UA Variable/Method materialization.
/// <para>
/// The class is split into per-theme partial files
/// (<c>WotConTests.Discovery.cs</c>, <c>WotConTests.CreateAsset.cs</c>, etc.) so each
/// plan item lands tests in its own file rather than growing a monolithic test class.
/// Shared helpers live in <c>WotConTests.Helpers.cs</c>.
/// </para>
/// </summary>
[TestFixture]
public partial class WotConTests : SimulatorTestsBase
{
    // NodeIds defined in the WoT-Con NodeSet (Opc.Ua.WotCon.NodeSet2.xml).
    // Namespace is OpcPlc.Namespaces.WotCon ("http://opcfoundation.org/UA/WoT-Con/");
    // index is server-assigned at runtime.
    private const uint WotAssetConnectionManagementObjectId = 31;
    private const uint CreateAssetMethodInstanceId = 32;
    private const uint DeleteAssetMethodInstanceId = 35;
    private const uint IWoTAssetTypeId = 42;
    private const uint FileCloseAndUpdateTypeMethodId = 111;

    public WotConTests() : base(["--wotcon"])
    {
    }

    private ushort WotConNamespaceIndex => (ushort)Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.WotCon);

    private NodeId WotConNodeId(uint identifier) => new(identifier, WotConNamespaceIndex);
}
