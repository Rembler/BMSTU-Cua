"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

connection.on("ReceiveNotification", function (message) {
    var holder = $("#notifications-holder");
    var notification = $("<div></div>").addClass("content-div my-queue-member-holder");
    var text = $("<p></p>").text(message);
    holder.append(notification.append(text));
    alert("You got new notification");
});

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
    pBody.textContent = `${message}`;
    pSender.textContent = `${user}`;
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});