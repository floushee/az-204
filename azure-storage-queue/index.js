const { QueueServiceClient } = require("@azure/storage-queue");
const connectionString = "";

const queueServiceClient = QueueServiceClient.fromConnectionString(connectionString);
const queueName = `floushee`;

async function main() {
    const queueClient = queueServiceClient.getQueueClient(queueName);

    console.log('Create queue');
    queueClient.createIfNotExists();

    console.log('Send messages');
    queueClient.sendMessage('message 1');
    queueClient.sendMessage('message 2');
    queueClient.sendMessage('message 3');

    console.log('Receive Messages');
    const response = await queueClient.receiveMessages();
    if (response.receivedMessageItems.length == 1) {
        const item = response.receivedMessageItems[0];
        
        console.log(item.messageText);
        
        await queueClient.deleteMessage(item.messageId, item.popReceipt);
    }

    console.log('Delete Queue');
    queueClient.deleteIfExists();
}

main().catch(err => console.log(err));