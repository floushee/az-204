const { ServiceBusClient, ReceiveMode } = require('@azure/service-bus'); 

const connectionString = '';
const topicName = ''; 
const subscriptionName = ''; 

async function main(){
  const client = ServiceBusClient.createFromConnectionString(connectionString); 
  const subscriptionClient = client.createSubscriptionClient(topicName, subscriptionName);
  const receiver = subscriptionClient.createReceiver(ReceiveMode.receiveAndDelete);

  try {
    const messages = await receiver.receiveMessages(10);
    console.log('Received messages:');
    console.log(messages.map(message => message.body));

    await subscriptionClient.close();
  } finally {
    await client.close();
  }
}

main().then(() => {
    console.log('Done');
}).catch((err) => {
    console.log(`An error occured: ${err}`);
});