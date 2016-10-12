<xsl:stylesheet version="1.0" 
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>

<xsl:template match="/formattedMeshNet">
    <graphml xmlns="http://graphml.graphdrawing.org/xmlns"  
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://graphml.graphdrawing.org/xmlns
     http://graphml.graphdrawing.org/xmlns/1.0/graphml.xsd">
        <graph id="G" edgedefault="undirected">
            <xsl:for-each select="contents/node">
                <xsl:variable name="parentNodeID" select="nodeID" /> 
                <node id="n{$parentNodeID}" />
                <xsl:for-each select="physNeighbors/int">
                    <edge source="n{$parentNodeID}" target="n{.}" />
                </xsl:for-each>
            </xsl:for-each>
        </graph>
     </graphml>
</xsl:template>

</xsl:stylesheet>