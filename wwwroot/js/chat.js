"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

//Disable send button until connection is established
document.getElementById("send-message").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var li = document.createElement("li");
    var div = document.createElement("div");
    div.classList.add("content-div", "my-msg-div");
    if (user == document.getElementById("current-user").textContent) {
        div.classList.add("my-own-msg-div");
    }
    var pSender = document.createElement("p");
    pSender.classList.add("my-msg-sender");
    var pBody = document.createElement("p");
    pBody.classList.add("my-msg-body");
    div.append(pSender, pBody);
    li.appendChild(div);
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    // li.textContent = `${user} says ${message}`;
    pBody.textContent = `${message}`;
    pSender.textContent = `${user}`;
});

connection.start().then(function () {
    var url = window.location.href;
    var roomId = url.substring(url.lastIndexOf('/') + 1);
    connection.invoke("JoinGroup", roomId);
    document.getElementById("send-message").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("send-message").addEventListener("click", function (event) {
    var message = document.getElementById("message").value;
    if (message != "") {
        var user = document.getElementById("current-user").textContent;
        var url = window.location.href;
        var roomId = url.substring(url.lastIndexOf('/') + 1);
        document.getElementById("message").value = "";
        connection.invoke("SendMessage", roomId, user, message).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    }
});