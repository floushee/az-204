const { ServiceBusClient } = require("@azure/service-bus");

const connectionString = '';
const topicName = ''; 

async function main() {

    const client = ServiceBusClient.createFromConnectionString(connectionString);
    const topicClient = client.createTopicClient(topicName);
    const sender = topicClient.createSender();

    try {
        for (let i = 0; i < 10; i++) {
            const message = {
                body: `Hello world! ${i}`,
                label: `test`,
                userProperties: {
                    myCustomPropertyName: `my custom property value ${i}`
                }
            };
            console.log(`Sending message: ${message.body}`);
            await sender.send(message);
        }

        await topicClient.close();
    } finally {
        await client.close();
    }
}

main().then(() => {
    console.log('Done');
}).catch((err) => {
    console.log(`An error occured: ${err}`);
});
