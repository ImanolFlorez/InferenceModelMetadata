(function ($, adapting) {

    var fieldsNS = adapting.namespace("adapting.extensions.fields");
    fieldsNS.InferenceModel = fieldsNS.InferenceModel || {
        ButtonRemove: function (urlController, fieldname, Code, modelo, urlIcono, MaxValue, fieldId, holdercode) {
            $(".InferenceModelButton_" + fieldname).show();
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Code: Code, MinValue: modelo, DefaultValue: urlIcono, MaxValue: MaxValue, Fieldname: fieldname, FieldId: fieldId, Holdercode: holdercode },
                success: function (data) {
                    console.log("ok");
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >");
                },
                error: function (response) {
                    console.log("error");
                    console.log(fieldname);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }
            });
        },
        ButtonCall: function (urlController, fieldname, Code, modelo, urlIcono, MaxValue, fieldId, holdercode) {
            $(".InferenceModelButton_" + fieldname).hide();
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Code : Code, MinValue : modelo, DefaultValue : urlIcono, MaxValue : MaxValue, Fieldname : fieldname, FieldId : fieldId, Holdercode : holdercode },
                success: function (data) {
                    console.log("ok");
                    $(".InferenceModelButton_" + fieldname).show();
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >");
                },
                error: function (response) {
                    console.log("error");
                    console.log(fieldname);
                    $(".InferenceModelButton_" + fieldname).show();
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }
            });

        },
        First: function (urlController, fieldname, Code, modelo, urlIcono, MaxValue, fieldId, holdercode) {

            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Code: Code, MinValue: modelo, DefaultValue: urlIcono, MaxValue: MaxValue, Fieldname: fieldname, FieldId: fieldId, Holdercode : holdercode },
                success: function (data) {
                    console.log("ok");
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >"); 
                },
                error: function (response) {
                    console.log("error");
                    console.log(fieldname);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");      
                }
            });

        },
        SelectCategory: function (cadena, urlController, fieldname, code, fieldid, modelo) {
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Cadena: cadena, FieldId: fieldid, Code: code, Fieldname: fieldname, Minvalue : modelo},
                success: function (data) {
                    console.log("ok");
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >");
                    $(".InferenceModelButton_" + fieldname).hide();
                },
                error: function (response) {
                    console.log("error");
                    console.log(response);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }  
            });
        },
        Confirmation: function (cadena, urlController, fieldname, code, fieldid, selection, modelo, holdercode) {
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Cadena: cadena, Selection: selection, Code: code, FielId: fieldid, Fieldname: fieldname, Minvalue: modelo, Holdercode: holdercode},
                success: function (data) {
                    console.log("ok");
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >"); 
                },
                error: function (response) {
                    console.log("error");
                    console.log(response);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }
            });
        },
        SelectSuper: function (cadena, urlController, fieldname, code, fieldid,modelo) {
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Cadena: cadena, FieldId: fieldid, Code: code, Fieldname: fieldname, Minvalue: modelo },
                success: function (data) {
                    console.log("ok");
                    $(".InferenceModelButton_" + fieldname).hide();
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >");
                },
                error: function (response) {
                    console.log("error");
                    console.log(response);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }
            });
        },
        Qualification: function (qualification, urlController, fieldname,code,fieldid,modelo) {
            $.ajax({
                url: urlController,
                type: 'post',
                dataType: 'json',
                data: { Qualification: qualification.value, FieldId: fieldid, Code: code, Fieldname: fieldname, Minvalue: modelo },
                success: function (data) {
                    console.log("ok");
                    $(".InferenceModelButton_" + fieldname).hide();
                    $("." + fieldname + "").html("<div id='TableModel'>" + data + "</div >");
                },
                error: function (response) {
                    console.log("error");
                    console.log(response);
                    $("." + fieldname + "").html("<div> ERROR: " + response.statusText + "," + response.status + "</div >");
                }
            });
            alert(value.value);
        },
        GraficaQualification:function(valor,maxneg,maxpos,id){
            var style = document.createElement('style');
            var value = 0;
            if (id == "sin_tono_sentimiento_pqrs") {
                if (valor <= 2) {
                    value = 20;
                } else if (valor > 2 && valor < 6) {
                    value = 50;
                } else {
                    value = 80;
                }
            } else {
                value = Math.round(valor*100);
            }
            
            style.innerHTML = `
            .rango_`+ id + `{
                width: 500px;
                height: 50px;
                background-image: url('/Content/Images/REGLA.png');
            }

            .Qualification_`+ id + `{
                width: 500px;
                padding-bottom: 40px;
                padding-top: 40px;
            }
            .izquierda_`+ id + `{
                float: left;
                background-color: #92000A;
                width: 30%;
                height: 9px;
                padding-left: 10px;
                margin-left: 14px;
            }
            .derecha_`+ id + `{
                float: right;
                background: #77DD77;
                width: 40%;
                height: 9px;
                margin-right: 9px;
            }
            .centro_`+ id + `{
                background: #E6E200;
                margin: 0 auto;
                width: 6.5%;
                height: 9px;
                margin-left: 176px;
                position: absolute;
            }
            .mal_`+ id + `{
                float: left;
                background-color: transparent;
                width: 31.9%;
                height: 28px;
                padding-left: 10px;
                background-image: url(/Content/Images/Insatisfecho1.png);
                background-repeat: no-repeat;
                background-position: left;
                background-size: 22% 106%;
            }
            .bien_`+ id + `{
                float: right;
                background-color: transparent;
                width: 30.8%;
                height: 28px;
                background-image: url(/Content/Images/Satisfecho1.png);
                background-repeat: no-repeat;
                background-position: right;
                background-size: 22% 100%;
            }
            .neutro_`+ id + `{
                background-color: transparent;
                margin: 0 auto;
                display: inline-block;
                width: 34%;
                height: 28px;
                background-repeat: no-repeat;
                background-position: center;
            }
            p.clasificacion {
                position: relative;
                overflow: hidden;
                display: inline-block;
            }
            p.clasificacion input {
                position: absolute;
                top: -100px;
            }
            p.clasificacion label {
                float: right;
                color: #333;
            }
            label{
                font-size: 200%;
            }
            p.clasificacion label:hover,
            p.clasificacion label:hover ~ label,
            p.clasificacion input:checked ~ label {
                color: rgb(0, 153, 255);
            }
            .flecha_`+ id + `{
                font-size: 150%;
                padding-left: `+ value + `%;
            }`;
            document.head.appendChild(style);
        },
        GraficaSelection(id, valor){
            $('.e-progress-txt').css({'top':'1px'});
            $('.' + id).ejProgressBar({
                value: valor,
                width: '100%',
                height: '100%'
            });
            var progress = $('.' + id).data('ejProgressBar');
            progress.setModel({ text: progress.getValue() + '%' });
        }

    };



}(jQuery, adapting));


