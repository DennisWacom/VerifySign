$(document).ready(function () {

    var docs = [];
    var docCount = 0;
    getData();

    function reset() {
        docs = [];
        docCount = 0;
        $("#docDiv").empty();
    }

    function loadDocumentListIfReady() {
        var data = {};
        data["documents"] = docs;

        var source = $("#doclist_template").html();
        var template = Handlebars.compile(source);
        
        $("#docDiv").html(template(data));
        
        $(".approve-btn").click(function () {
            
            var approver = $(this).closest('tr').children('td.approver').text();
            var approverEmail = $(this).closest('tr').children('td.approverEmail').text();
            var filehref = $(this).closest('tr').children('td.filename').find('a').attr('href');
            var id = parseInt($(this).closest('tr').attr('id'));

            $.ajax({
                type: 'GET',
                url: 'http://localhost:7801/approve',
                contentType: 'text/plain',
                xhrFields: { withCredentials: false },
                data: { 'name': approver, 'email': approverEmail, 'file': filehref },
                headers: {},
                success: function (data, textStatus, xhr) {
                    if (data == "") {
                        alert("Action cancelled");
                        return;
                    }
                    alert("Document Approved");
                    docs[id].filename = data;
                    docs[id].status = "Approved";
                    docs[id].isPending = false;
                    docs[id].isApproved = true;
                    docs[id].isVerified = false;
                    reset();
                    getData();
                },
                error: function (request, textStatus, errorThrown) {
                    alert(textStatus);
                }
            });
        });

        $(".verify-btn").click(function () {
            
            var verifier = $(this).closest('tr').children('td.verifier').text();
            var verifierEmail = $(this).closest('tr').children('td.verifierEmail').text();
            var filehref = $(this).closest('tr').children('td.filename').find('a').attr('href');
            var id = parseInt($(this).closest('tr').attr('id'));

            $.ajax({
                type: 'GET',
                url: 'http://localhost:7801/verify',
                contentType: 'text/plain',
                xhrFields: { withCredentials: false },
                data: { 'name': verifier, 'email': verifierEmail, 'file': filehref },
                headers: {},
                success: function (data, textStatus, xhr) {
                    if (data == "") {
                        alert("Action cancelled");
                        return;
                    }
                    alert("Document Verified");
                    docs[id].filename = data;
                    docs[id].status = "Verified";
                    docs[id].isPending = false;
                    docs[id].isApproved = false;
                    docs[id].isVerified = true;
                    reset();
                    getData();
                },
                error: function (request, textStatus, errorThrown) {
                    alert(textStatus);
                }
            });
        });
    }

    function setStatus(doc, status) {

        var tmpName = doc["filename"];
        var ext = tmpName.substr(tmpName.length - 4, 4);
        var statusText = "";
        if(status == "Approved"){
            statusText = "_approved";
        }else if(status == "Verified"){
            statusText = "_approved_verified";
        }
        tmpName = tmpName.substr(0, tmpName.length - 4) + statusText + ext;
        
        if (status == "Pending") {
            doc["isPending"] = true;
            doc["isApproved"] = false;
            doc["isVerified"] = false;
        } else if (status == "Approved") {
            doc["isPending"] = false;
            doc["isApproved"] = true;
            doc["isVerified"] = false;
            doc["filename"] = tmpName;
        } else if (status == "Verified") {
            doc["isPending"] = false;
            doc["isApproved"] = false;
            doc["isVerified"] = true;
            doc["filename"] = tmpName;
        } else {
            doc["isPending"] = false;
            doc["isApproved"] = false;
            doc["isVerified"] = false;
        }
    }

    function getData() {
        var totalFiles = 2;
        
        var currentPath = window.location.href;
        currentPath = currentPath.substr(0, currentPath.lastIndexOf('/'));
        var docsPath = currentPath + "/docs/";

        var docA = {};
        docA["id"] = 0;
        docA["display"] = "DocumentA.pdf";
        docA["filename"] = "docs/DocumentA.pdf";
        docA["approver"] = "Dennis Cai";
        docA["approverEmail"] = "dennis.cai@wacom.com";
        docA["verifier"] = "Aaron Johnson";
        docA["verifierEmail"] = "aaron.johnson@wacom.com";

        $.ajax({
            type: 'GET',
            url: 'http://localhost:7801/status',
            contentType: 'text/plain',
            xhrFields: { withCredentials: false },
            data: { 'file': docA["filename"] },
            headers: {},
            success: function (data, textStatus, xhr) {
                if (data == "") {
                    alert("Action cancelled");
                    return;
                }
                
                docA["status"] = data;
                setStatus(docA, data);
                docs[docCount] = docA;
                docCount++;
                if (docCount >= totalFiles) {
                    loadDocumentListIfReady(docs);
                }
            },
            error: function (request, textStatus, errorThrown) {
                alert(textStatus);
            }
        });


        var docB = {};
        docB["id"] = 1;
        docB["display"] = "DocumentB.pdf";
        docB["filename"] = "docs/DocumentB.pdf";
        docB["approver"] = "Dennis Cai";
        docB["approverEmail"] = "dennis.cai@wacom.com";
        docB["verifier"] = "Aaron Johnson";
        docB["verifierEmail"] = "aaron.johnson@wacom.com";
        
        $.ajax({
            type: 'GET',
            url: 'http://localhost:7801/status',
            contentType: 'text/plain',
            xhrFields: { withCredentials: false },
            data: { 'file': docB["filename"] },
            headers: {},
            success: function (data, textStatus, xhr) {
                if (data == "") {
                    alert("Action cancelled");
                    return;
                }
                
                docB["status"] = data;
                setStatus(docB, data);
                docs[docCount] = docB;
                docCount++;
                
                if (docCount >= totalFiles) {
                    loadDocumentListIfReady(docs);
                }
            },
            error: function (request, textStatus, errorThrown) {
                alert(textStatus);
            }
        });

        return result;
    }

    

});

