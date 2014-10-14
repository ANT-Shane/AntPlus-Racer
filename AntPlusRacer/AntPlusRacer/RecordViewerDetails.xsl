<!--
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
-->
<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html"/>

<xsl:template match="/">
     <xsl:for-each select="RecordDatabase/recordBook/TrackRecordList">
     <xsl:sort select="sportType"/>
     <xsl:sort select="trackDistance"/>
     <h2><xsl:value-of select="trackDistance"/>m <xsl:value-of select="sportType"/> Records</h2>
       <table border="0">
         <tr bgcolor="#9acd32">
           <th>Name</th>
           <th>Record (s)</th>
           <th>Time of Record</th>
           <th>Data Src Used</th>
           <th class="ContactInfo">Phone</th>
           <th class="ContactInfo">Email</th>
         </tr>
         <xsl:for-each select="trackRecords/RecordData">
         <tr>
           <td><xsl:value-of select="FirstName"/><xsl:text> </xsl:text><xsl:value-of select="LastName"/></td>
           <td><xsl:value-of select="recordValue"/></td>
           <td><xsl:value-of select="recordDate"/></td>
           <td><xsl:value-of select="DataSourceName"/></td>
           <td class="ContactInfo"><xsl:value-of select="PhoneNumber"/></td>
           <td class="ContactInfo"><xsl:value-of select="Email"/></td>        
         </tr>
         </xsl:for-each>
       </table>
     </xsl:for-each>
</xsl:template>
</xsl:stylesheet>