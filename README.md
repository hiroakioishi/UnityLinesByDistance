# UnityLinesByDistance

## Description
頂点同士の距離によってラインを結ぶもののサンプルコード

ComputeShader内で頂点同士の距離を計算し, AppendStructuredBufferにラインのデータをAppend, そのデータバッファをもとにGeometryShaderで頂点同士を結んでラインを描画しています. 生成されるラインの総数を制限する目的で, 総和計算にCounterBufferを使用していますが, 知識が足らず適切な実装とは言えないため, ご使用は自己責任でお願いします.

![image](https://github.com/hiroakioishi/UnityLinesByDistance/blob/master/img.jpg)

## Demo
https://youtu.be/4tf1jCZ_ouE


## Requirement
ComputeShader, GeometryShaderを使用しているため、DirectX11が動作する環境が必要です
