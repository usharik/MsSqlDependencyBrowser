﻿document.addEventListener('DOMContentLoaded', onDocumentReady, false);

function onDocumentReady() {
    buildObjectListPanel();
}

function postConnectionString() {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/connect');
    xhr.setRequestHeader('Content-Type', 'application/json')
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {
                location.reload();
            } else if (xhr.status == 406) {
                var error = JSON.parse(xhr.response);
                document.getElementById("errorMessage").innerHTML = error.errorMessage;
                document.getElementById("btConnect").disabled = false;
                document.getElementById("btCancel").disabled = false;
            }
        }
    };
    document.getElementById("btConnect").disabled = true;
    document.getElementById("btCancel").disabled = true;
    
    xhr.send(JSON.stringify({
        server: document.getElementById("server").value,
        database: document.getElementById("database").value
    }));
}

function selectAllText(containerid) {
    if (document.selection) { 
        var range = document.body.createTextRange();
        range.moveToElementText(document.getElementById(containerid));
        range.select().createTextRange();
    } else if (window.getSelection) {
        var range = document.createRange();
         range.selectNode(document.getElementById(containerid));
         window.getSelection().removeAllRanges();
         window.getSelection().addRange(range);
    }
}

function buildObjectListPanel() {
    var comboBoxItems = allServerObjects.map(function (objList) {
        return "<option value='" + objList.type_desc + "'>" + objList.type_desc + "</option>";
    });
    document.getElementById("objectTypeComboBox").innerHTML = comboBoxItems.join('');
    objectTypeComboBoxChange();
}

function objectTypeComboBoxChange() {
    var comboBox = document.getElementById("objectTypeComboBox");
    var sel_type_desc = comboBox.options[comboBox.selectedIndex].value;
    var objectList = allServerObjects.filter(function (obj) {
        return obj.type_desc == sel_type_desc;
    });

    document.getElementById("objectList").innerHTML =
        objectList[0].objects.map(function (obj) {
        return "<li>" + buildServerObjectLink(obj) + "</li>";
        }).join('');
}

function buildServerObjectLink(objectName) {
    return "<a href=" + location.protocol + "//" + location.host + "/?sp=" + objectName + " onclick='getObjText(event)'>" + objectName + "</a>";
}

function getObjText(event) {
    var link = event.target;
    var xhr = new XMLHttpRequest();
    xhr.open("GET", "/objtext?sp=" + link.textContent);
    xhr.setRequestHeader("Content-Type", "application/json")
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {
                document.getElementById("text").innerHTML = xhr.response;
            }
        }
    };
    xhr.send();
    event.preventDefault();
}