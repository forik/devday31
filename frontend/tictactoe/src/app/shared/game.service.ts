import { Injectable, EventEmitter } from '@angular/core';

declare var signalR;

@Injectable()
export class GameService {
  private connection: any;

  onMoveMade = new EventEmitter<any>();

  constructor() {
    const transport = signalR.TransportType.WebSockets;
    const logger = new signalR.ConsoleLogger(signalR.LogLevel.Information);
    this.connection = new signalR.HubConnection('/gameHub', {
      transport: transport,
      logger: logger
    });

    this.connection.on('OnMoveMade', data => this.onMoveMade.emit(data));

    this.connection.start();
  }

  makeMove(gameId: string, x: number, y: number) {
    this.connection.invoke('makeMove', gameId, x, y);
  }
}
