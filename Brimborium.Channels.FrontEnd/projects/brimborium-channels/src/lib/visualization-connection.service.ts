import { inject, Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { Disposable, getHubProxyFactory, getReceiverRegister } from "./generated/TypedSignalR.Client";
import { IBCVisualizationHub, IBCVisualizationReveiver } from './generated/TypedSignalR.Client/Brimborium.Channels.Hubs';
import { BCDescriptionGraph, BCLogRecord } from './generated/Brimborium.Channels';
import { R } from '@angular/cdk/keycodes';

@Injectable({
  providedIn: 'root',
})
export class VisualizationConnectionService {
  private _connection: HubConnection | undefined;
  private _hubProxy: IBCVisualizationHub| undefined;
  private _subscriptionReveiver: Disposable| undefined;
  
  public $lastError = signal<string>('', { debugName: '$lastError' });

  constructor() {
  }

  ensureConnected(): void {
    window.requestAnimationFrame(() => {
      this.ensureConnectedAsync().catch((err: any) => {
        console.error(err);
        if (err == null){
          this.$lastError.set('Error');
        } else {
          this.$lastError.set(err.toString());
        }
      });
    });
  }

  async ensureConnectedAsync() {
    if (this._connection == null) {
      const hubLocation = location.protocol + "//" + location.host + "/_hubs/BCVisualizationHub";
      
      const connection = new HubConnectionBuilder()
        .withUrl(hubLocation)
        .build();
      this._connection = connection;

      const receiver: IBCVisualizationReveiver = {
        onJoin: async (): Promise<void> => { return; },
        onLeave: async (): Promise<void> => { return; },
        onMessage: async (listLogRecords: BCLogRecord[]): Promise<void> => { return; }
      }

      const hubProxy = getHubProxyFactory("IBCVisualizationHub").createHubProxy(connection);
      const subscriptionReveiver = getReceiverRegister("IBCVisualizationReveiver").register(connection, receiver);

      await connection.start();
      this._hubProxy = hubProxy;
      this._subscriptionReveiver = subscriptionReveiver;

      hubProxy.descriptionGraphChannel()
        .subscribe({
          next: (value: BCDescriptionGraph) => {

          },
          error: (err: any) => {

          },
          complete: (): void => {

          }
        });

      debugger;
      const graphRoot = await hubProxy.getDescriptionGraph("root");
      console.log(graphRoot);

      //await hubProxy.join()

      //const participants = await hubProxy.getParticipants()
    }

    return this._hubProxy!;
  }

  public async getDescriptionGraph(graphId: string){
    const hubProxy = await this.ensureConnectedAsync();
    const graph = await hubProxy.getDescriptionGraph(graphId);
    return graph;
  }
}
