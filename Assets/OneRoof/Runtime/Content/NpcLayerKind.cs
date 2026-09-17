namespace OneRoof.Content
{
    /// <summary>
    /// The canonical 8-layer order for NPC visual assembly mandated by Docs/05_ASSET_PIPELINE.md:
    /// body, face, hair, lower clothing, upper clothing, footwear, accessory, carried prop.
    /// </summary>
    public enum NpcLayerKind
    {
        Body = 0,
        Face = 1,
        Hair = 2,
        LowerClothing = 3,
        UpperClothing = 4,
        Footwear = 5,
        Accessory = 6,
        CarriedProp = 7
    }
}
