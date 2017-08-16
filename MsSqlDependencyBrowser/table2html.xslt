<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
    <xsl:output method="xml" indent="yes"/>  
    <xsl:template match="/">
      <table>
        <tr>
          <th>№</th>
          <th>Column Name</th>
          <th>Type Name</th>
          <th>Max Length</th>
          <th>Precision</th>
          <th>Is nullable</th>
          <th>Is identity</th>
        </tr>
      <xsl:for-each select="table/column">
        <tr>
          <td>
            <xsl:value-of select="@columnOrder"/>
          </td>
          <td>
            <xsl:value-of select="@columnName"/>
          </td>
          <td>
            <xsl:value-of select="@typeName"/>
          </td>
          <td>
            <xsl:value-of select="@maxLength"/>
          </td>
          <td>
            <xsl:value-of select="@precision"/>
          </td>
          <td>
            <xsl:value-of select="@is_nullable"/>
          </td>
          <td>
            <xsl:value-of select="@is_identity"/>
          </td>
        </tr>
      </xsl:for-each>
      </table>
    </xsl:template>
</xsl:stylesheet>
