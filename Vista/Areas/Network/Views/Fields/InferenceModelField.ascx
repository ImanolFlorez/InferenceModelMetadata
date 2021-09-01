<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl<Adapting.Network.Web.Models.Contents.FieldViewModel>" %>
<%@ Import Namespace="WebserviceMetadata" %>
<script type="text/javascript">

    function updateDefaultValue(type) {
       
        var url = $("#" + type + "Text").val();
        var holdercode = $("#" + type + "Text2").val();

        $("#InferenceModelDefaultValue").val(url + '|' + holdercode );
    }


</script>
<div>
  <table class="grid">
          <tr id="trDefaultValue" >
          <td class="tcolumna1">EndPoint Modelo Inferencia: </td>
          <td class="tcolumna2">
              <span id="Percentag+eFieldDefaultValueSpan" >              
                   <%=Html.TextBox(Model.Item.Type + "Text", InferenceModelField.GetPosition(Model.Item.DefaultValue, 0), new {onkeyup="updateDefaultValue('"+Model.Item.Type+"')"})%>
                  <%=Html.ValidationMessage("InferenceModelDefaultValue")%>
              </span>          
          </td>
          </tr>
       <tr id="trDefaultValue" >
          <td class="tcolumna1">Codigo Holder: </td>
          <td class="tcolumna2">
              <span id="Percentag+eFieldDefaultValueSpan" >              
                  <%=Html.TextBox(Model.Item.Type + "Text2", InferenceModelField.GetPosition(Model.Item.DefaultValue, 1), new {onkeyup="updateDefaultValue('"+Model.Item.Type+"')"})%>
               <%= Html.Hidden("InferenceModelDefaultValue", Model.Item.DefaultValue) %>
              </span>          
          </td>
          </tr>
          <tr id="<%= Model.Item.Type %>Max" >
                <td class="tcolumna1">Numero de resultados maximos del modelo:</td>
                <td class="tcolumna2">
                    <%= Html.TextBox(Model.Item.Type + "MaxValue", Model.Item.MaxValue, new { style = "width:40px" })%>  
                    <%=Html.ValidationMessage(Model.Item.Type + "MaxValue")%>
                </td>
          </tr>  
            <tr id="<%= Model.Item.Type %>Min" >
                <td class="tcolumna1">Nombre del modelo de inf:</td>
                <td class="tcolumna2">
                    <%= Html.TextBox(Model.Item.Type + "MinValue", Model.Item.MinValue, new { style = "width:150px" })%>  
                    <%=Html.ValidationMessage(Model.Item.Type + "MinValue")%>
                </td>
          </tr>  
  </table>

     
</div>
